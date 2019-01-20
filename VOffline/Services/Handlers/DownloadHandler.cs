using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VOffline.Models.Storage;
using VOffline.Services.Queues;

namespace VOffline.Services.Handlers
{
    class DownloadHandler : IHandler<IDownload>
    {
        private readonly QueueProvider queueProvider;

        public DownloadHandler(QueueProvider queueProvider)
        {
            this.queueProvider = queueProvider;
        }

        public Task<IEnumerable<object>> Process(IDownload download, CancellationToken token, ILog log)
        {
            queueProvider.Downloads.Add(download);
            return Task.FromResult(Enumerable.Empty<object>());
        }
    }
}