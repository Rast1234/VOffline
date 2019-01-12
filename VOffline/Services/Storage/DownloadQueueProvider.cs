using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using VOffline.Models.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Storage
{
    public class DownloadQueueProvider
    {
        public DownloadQueueProvider(ConstantsProvider constantsProvider)
        {
            Pending = new AsyncProducerConsumerQueue<IDownload>(constantsProvider.DownloadQueueLimit);
        }

        public async Task EnqueueAll(IEnumerable<IDownload> items, CancellationToken token)
        {
            await Task.WhenAll(items.Select(async x => await Pending.EnqueueAsync(x, token)));
        }

        public AsyncProducerConsumerQueue<IDownload> Pending { get; }
    }
}