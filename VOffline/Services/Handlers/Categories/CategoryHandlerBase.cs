using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers.Categories
{
    public abstract class CategoryHandlerBase<T> : IHandler<Nested<T>>
    {
        protected readonly FileSystemTools fileSystemTools;

        protected CategoryHandlerBase(FileSystemTools fileSystemTools)
        {
            this.fileSystemTools = fileSystemTools;
        }

        public async Task<IEnumerable<object>> Process(Nested<T> nested, CancellationToken token, ILog log)
        {
            var workDir = fileSystemTools.CreateSubdir(nested.ParentDir, nested.Name, CreateMode.OverwriteExisting);
            return await ProcessInternal(nested.Data, workDir, token, log);
            
            // TODO: move this to processing code:
            //log.Info($"Completed category [{category.WorkDir.FullName}]");
            //log.Error($"Failed {GetType().FullName} while working in [{category.WorkDir.FullName}]. Data is:\n{JsonConvert.SerializeObject(category)}", null);
        }

        public abstract Task<IEnumerable<object>> ProcessInternal(T category, DirectoryInfo workDir, CancellationToken token, ILog log);
    }
}