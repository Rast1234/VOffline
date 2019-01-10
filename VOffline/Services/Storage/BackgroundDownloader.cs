using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Nito.AsyncEx;
using RestSharp;
using VkNet;
using VOffline.Models.Storage;
using VOffline.Services.Handlers;

namespace VOffline.Services.Storage
{
    public class BackgroundDownloader
    {
        private readonly DownloadQueueProvider queueProvider;
        private readonly FilesystemTools filesystemTools;

        private readonly VkApi vkApi;
        //private readonly int retryLimit;

        public BackgroundDownloader(DownloadQueueProvider queueProvider, FilesystemTools filesystemTools, VkApi vkApi)
        {
            this.queueProvider = queueProvider;
            this.filesystemTools = filesystemTools;
            this.vkApi = vkApi;
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
                    var content = await item.GetContent(vkApi, token);
                    var file = filesystemTools.CreateUniqueFile(item.Location, item.DesiredName);
                    await File.WriteAllBytesAsync(file.FullName, content, token);
                    success++;
                    log.Info($"Saved [{item.DesiredName}] as [{file.FullName}] with [{content.Length}] bytes");
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    item.AddError(e);
                    await errors.EnqueueAsync(item, token);
                    log.Warn($"Error downloading {item.DesiredName}", e);
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