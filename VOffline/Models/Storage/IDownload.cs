using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace VOffline.Models.Storage
{
    public interface IDownload
    {
        DirectoryInfo Location { get; }
        string DesiredName { get; }
        Task<byte[]> GetContent(CancellationToken token);
    }
}