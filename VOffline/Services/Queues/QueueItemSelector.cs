using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace VOffline.Services.Queues
{
    public delegate Task<IEnumerable<TData>> QueueItemSelector<TData>(TData data, long i, CancellationToken token, ILog log);
}