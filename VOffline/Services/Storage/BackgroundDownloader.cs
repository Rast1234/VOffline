using System;
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

        public BackgroundDownloader(FileSystemTools fileSystemTools)
        {
            this.fileSystemTools = fileSystemTools;
        }

        public async Task<IEnumerable<IDownload>> DownloadData(IDownload data, long i, CancellationToken token, ILog log)
        {
            await fileSystemTools.WriteFileWithCompletionMark(data.Location, data.DesiredName, async () => await data.GetContent(token), token, log);
            return Nothing;
        }

        private static readonly IEnumerable<IDownload> Nothing = Enumerable.Empty<IDownload>();
    }

    public class JobProcessor
    {
        private readonly FileSystemTools fileSystemTools;

        public JobProcessor(FileSystemTools fileSystemTools)
        {
            this.fileSystemTools = fileSystemTools;
        }

        public async Task<IEnumerable<object>> ProcessJob(object job, long i, CancellationToken token, ILog log)
        {
            throw new NotImplementedException();
        }

    }
}