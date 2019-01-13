using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers
{
    public abstract class HandlerBase<T>
    {
        protected readonly FilesystemTools filesystemTools;

        protected HandlerBase(FilesystemTools filesystemTools)
        {
            this.filesystemTools = filesystemTools;
        }

        public async Task Process(T data, DirectoryInfo parentDir, CancellationToken token, ILog log)
        {
            DirectoryInfo workDir = null;
            try
            {
                token.ThrowIfCancellationRequested();  // this helps stop synchronous stuff inside long nested loops
                workDir = GetWorkingDirectory(data, parentDir);
                if (IsCompletable)
                {
                    if (filesystemTools.IsCompleted(workDir))
                    {
                        log.Info($"Skipping [{workDir.FullName}] because marked as competed");
                        return;
                    }
                }
                

                await ProcessInternal(data, workDir, token, log);
                if (IsCompletable)
                {
                    filesystemTools.MarkAsCompleted(workDir);
                }

                var completedText = IsCompletable
                    ? ", marked"
                    : "";
                log.Info($"Completed [{workDir.FullName}]{completedText}");

            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Error($"Failed {GetType().FullName} while working in [{workDir?.FullName}]. Data is:\n{JsonConvert.SerializeObject(data)}", e);
            }
        }

        public abstract Task ProcessInternal(T data, DirectoryInfo workDir, CancellationToken token, ILog log);

        public abstract DirectoryInfo GetWorkingDirectory(T data, DirectoryInfo parentDir);

        protected virtual bool IsCompletable { get; } = false;
    }
}