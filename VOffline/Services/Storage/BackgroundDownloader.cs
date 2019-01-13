using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Nito.AsyncEx;
using VOffline.Models.Storage;

namespace VOffline.Services.Storage
{
    public class BackgroundDownloader
    {
        private readonly DownloadQueueProvider queueProvider;
        private readonly FilesystemTools filesystemTools;

        public BackgroundDownloader(DownloadQueueProvider queueProvider, FilesystemTools filesystemTools)
        {
            this.queueProvider = queueProvider;
            this.filesystemTools = filesystemTools;
        }

        public async Task<List<IDownload>> Process(CancellationToken token, ILog log)
        {
            log.Debug($"{nameof(BackgroundDownloader)} started");
            var errors = new AsyncProducerConsumerQueue<IDownload>();
            IDownload item;
            int success;

            for(success=0; (item = await GetItem(token)) != null; success++)
            {
                // TODO: add second queue, semaphore and support for retries
                try
                {
                    await filesystemTools.WriteFileWithCompletionMark(item.Location, item.DesiredName, async () => await item.GetContent(token), token, log);
                    success++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    item.AddError(e);
                    await errors.EnqueueAsync(item, token);
                    log.Warn($"Error downloading {item}", e);
                }
            }

            errors.CompleteAdding();
            var errorList = errors.GetConsumingEnumerable(token).ToList();
            log.Info($"All downloads completed. {success} successful, {errorList.Count} failed");
            return errorList;
        }

        private async Task<IDownload> GetItem(CancellationToken token)
        {
            try
            {
                return await queueProvider.Pending.DequeueAsync(token);
            }
            catch (InvalidOperationException)
            {
            }

            return null;
        }
    }
}