using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace VOffline.Services.Handlers
{
    public interface IHandler
    {
        Task Process(DirectoryInfo parentDir, CancellationToken token, ILog log);
    }
}