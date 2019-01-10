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

        public AsyncProducerConsumerQueue<IDownload> Pending { get; }
    }
}