using Microsoft.Extensions.Options;
using VOffline.Models;
using VOffline.Models.Storage;

namespace VOffline.Services.Queues
{
    public class QueueProvider
    {
        public QueueProvider(IOptionsSnapshot<Settings> settings)
        {
            Downloads = new ParallelWorker<IDownload>(settings.Value.ParallelDownloadsLimit, settings.Value.DownloadsErrorLimit);
            Jobs = new ParallelWorker<object>(0, settings.Value.TaskErrorLimit);
        }

        public ParallelWorker<IDownload> Downloads { get; }
        public ParallelWorker<object> Jobs { get; }
    }
}