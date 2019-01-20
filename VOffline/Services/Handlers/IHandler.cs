using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace VOffline.Services.Handlers
{
    public interface IHandler<T>
    {
        Task<IEnumerable<object>> Process(T attachment, CancellationToken token, ILog log);
    }
}