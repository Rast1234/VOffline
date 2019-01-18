using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace VOffline.Services.Walkers
{
    public interface IWalker<T>
    {
        Task Process(T data, DirectoryInfo parentDir, CancellationToken token, ILog log);
        DirectoryInfo GetWorkingDirectory(T data, DirectoryInfo parentDir);
    }
}