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
        int RetryCount { get; }
        IReadOnlyList<Exception> Errors { get; }
        void AddError(Exception e);
        Task<byte[]> GetContent(CancellationToken token);
    }
}