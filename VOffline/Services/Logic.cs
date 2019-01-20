using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using VkNet;
using VkNet.Abstractions;
using VkNet.Model;
using VOffline.Models;
using VOffline.Models.Storage;
using VOffline.Services.Handlers;
using VOffline.Services.Queues;
using VOffline.Services.Storage;
using VOffline.Services.Token;
using VOffline.Services.Vk;

namespace VOffline.Services
{
    public class Logic
    {
        private readonly TokenMagic tokenMagic;
        private readonly IVkApi vkApi;
        private readonly VkApiUtils vkApiUtils;
        private readonly BackgroundDownloader downloader;
        private readonly Reflector reflector;
        private readonly FileSystemTools fileSystemTools;
        private readonly QueueProvider queueProvider;
        private readonly ConsoleProgress consoleProgress;
        private readonly Settings settings;

        public Logic(TokenMagic tokenMagic, IVkApi vkApi, VkApiUtils vkApiUtils, BackgroundDownloader downloader, Reflector reflector, FileSystemTools fileSystemTools, QueueProvider queueProvider, ConsoleProgress consoleProgress, IOptionsSnapshot<Settings> settings)
        {
            this.tokenMagic = tokenMagic;
            this.vkApi = vkApi;
            this.vkApiUtils = vkApiUtils;
            this.downloader = downloader;
            this.reflector = reflector;
            this.fileSystemTools = fileSystemTools;
            this.queueProvider = queueProvider;
            this.consoleProgress = consoleProgress;
            this.settings = settings.Value;
        }

        public async Task Run(CancellationToken token, ILog log)
        {
            log.Debug("Started");

            await Prepare(log);

            var modes = settings.GetWorkingModes();
            var identifiers = await GetIdentifiers();
            log.Info($"Processing modes  : {JsonConvert.SerializeObject(modes)}");
            log.Info($"Processing targets: {string.Join(", ", identifiers.Select(x => $"[{x.name} {x.id}]"))}");
            try
            {
                foreach (var identifier in identifiers)
                {
                    var name = await vkApiUtils.GetName(identifier.id);
                    var workDir = fileSystemTools.CreateSubdir(fileSystemTools.RootDir, name, CreateMode.OverwriteExisting);
                    log.Info($"id [{identifier.id}], name [{name}], path [{workDir.FullName}]");
                    foreach (var mode in modes)
                    {
                        AddJob(identifier.id, workDir, mode);
                    }
                }

                await WaitUntilDone(token, log);
            }
            catch (OperationCanceledException)
            {
                log.Warn($"Abandoned {queueProvider.Jobs.Count} pending jobs");
                log.Warn($"Abandoned {queueProvider.Downloads.Count} pending downloads");
            }
            finally
            {
                fileSystemTools.SaveCache(log);
            }
        }

        private async Task<IReadOnlyList<(string name, long id)>> GetIdentifiers()
        {
            var idTasks = settings.Targets
                .Select(async x =>
                {
                    var id = await vkApiUtils.ResolveId(x);
                    return (name: x, id: id);
                });
            var allIds = await Task.WhenAll(idTasks);
            var identifiers = allIds
                .Distinct()
                .ToImmutableList();
            return identifiers;
        }

        private async Task Prepare(ILog log)
        {
            // TODO: async token retrieval
            var vkToken = tokenMagic.GetTokenFromScratch(log);

            await vkApi.AuthorizeAsync(new ApiAuthParams()
            {
                AccessToken = vkToken.Token
            });

            fileSystemTools.LoadCache(log);
        }

        private async Task WaitUntilDone(CancellationToken token, ILog log)
        {
            queueProvider.Jobs.CompleteAdding();
            var jobsTask = queueProvider.Jobs.Start(reflector.ProcessJob, token, log);
            var downloaderTask = queueProvider.Downloads.Start(downloader.DownloadData, token, log);
            var progressTask = consoleProgress.BackgroundUpdate(TimeSpan.FromSeconds(0.5), token);

            await jobsTask;
            queueProvider.Downloads.CompleteAdding();
            await downloaderTask;
            consoleProgress.Stop();
            await progressTask;

            foreach (var jobError in queueProvider.Jobs.Failed)
            {
                log.Warn($"Failed job: {jobError.Data}", jobError.Errors.Value.LastOrDefault());
            }
            foreach (var downloadError in queueProvider.Downloads.Failed)
            {
                log.Warn($"Failed download: {downloadError.Data.DesiredName}", downloadError.Errors.Value.LastOrDefault());
            }
        }

        private void AddJob(long id, DirectoryInfo workDir, Mode mode)
        {
            // TODO: save raw requests/responses for future use?
            switch (mode)
            {
                case Mode.Wall:
                    queueProvider.Jobs.Add(new Nested<WallCategory>(new WallCategory(id), workDir, "Wall"));
                    break;
                case Mode.Audio:
                    queueProvider.Jobs.Add(new Nested<AudioCategory>(new AudioCategory(id), workDir, "Audio"));
                    break;
                case Mode.Photos:
                    queueProvider.Jobs.Add(new Nested<PhotoCategory>(new PhotoCategory(id), workDir, "Photo"));
                    break;
                case Mode.All:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "This mode should have been replaced before processing");
                default:
                    throw new NotImplementedException();
            }
        }
    }
}