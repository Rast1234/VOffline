using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Nito.AsyncEx;
using VOffline.Models.Storage;
using VOffline.Services.Queues;

namespace VOffline.Services.Storage
{
    public class BackgroundDownloader
    {
        private readonly FileSystemTools fileSystemTools;

        public long ProcessedBytes {get; private set; }

        public BackgroundDownloader(FileSystemTools fileSystemTools)
        {
            this.fileSystemTools = fileSystemTools;
        }

        public async Task<IEnumerable<IDownload>> DownloadData(IDownload data, long i, CancellationToken token, ILog log)
        {
            var contentLength = await fileSystemTools.WriteFileWithCompletionMark(data.Location, data.DesiredName, async () => await data.GetContent(token), token, log);
            ProcessedBytes += contentLength;
            return Nothing;
        }

        private static readonly IEnumerable<IDownload> Nothing = Enumerable.Empty<IDownload>();
    }
}