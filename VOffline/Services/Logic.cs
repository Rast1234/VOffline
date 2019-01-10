﻿using System;
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
using VkNet.Model;
using VkNet.Model.RequestParams;
using VOffline.Models;
using VOffline.Models.Storage;
using VOffline.Services.Handlers;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services
{
    public class Logic
    {
        private readonly Settings settings;
        private readonly TokenMagic tokenMagic;
        private readonly VkApi vkApi;
        private readonly VkApiUtils vkApiUtils;
        private readonly BackgroundDownloader downloader;
        private readonly FilesystemTools filesystemTools;
        private readonly AudioHandler audioHandler;

        public Logic(TokenMagic tokenMagic, VkApi vkApi, VkApiUtils vkApiUtils, BackgroundDownloader downloader, FilesystemTools filesystemTools, AudioHandler audioHandler, IOptionsSnapshot<Settings> settings)
        {
            this.settings = settings.Value;
            this.tokenMagic = tokenMagic;
            this.vkApi = vkApi;
            this.vkApiUtils = vkApiUtils;
            this.downloader = downloader;
            this.filesystemTools = filesystemTools;
            this.audioHandler = audioHandler;
        }

        public async Task Run(CancellationToken token, ILog log)
        {
            log.Debug("Started");

            // TODO: async token retrieval
            var vkToken = tokenMagic.GetTokenFromScratch(log);

            await vkApi.AuthorizeAsync(new ApiAuthParams()
            {
                AccessToken = vkToken.Token
            });


            var modes = settings.GetWorkingModes();
            var ids = settings.Targets
                .Select(async x => await vkApiUtils.ResolveId(x))
                .Select(x => x.Result)
                .Distinct()
                .ToImmutableHashSet();
            log.Debug($"Processing {JsonConvert.SerializeObject(modes)} for {JsonConvert.SerializeObject(ids)}");
            var downloaderTask = downloader.Process(token, log);

            var rootDir = filesystemTools.MkDir(settings.OutputPath);
            foreach (var id in ids)
            {
                
                var name = await vkApiUtils.GetName(id);
                var workDir = filesystemTools.CreateSubdir(rootDir, name, false);
                log.Info($"id [{id}], name [{name}], path [{workDir.FullName}]");
                foreach (var mode in modes)
                {
                    var modeDir = filesystemTools.CreateSubdir(workDir, mode.ToString(), false);
                    await ProcessTarget(id, modeDir, mode, token, log);
                }
            }

            var downloadErrors = await downloaderTask;
            foreach (var downloadError in downloadErrors)
            {
                log.Warn($"Failed {downloadError.DesiredName}", downloadError.Errors.LastOrDefault());
            }
        }

        private async Task ProcessTarget(long id, DirectoryInfo dir, Mode mode, CancellationToken token, ILog log)
        {
            switch (mode)
            {
                case Mode.Wall:
                    break;
                case Mode.Audio:
                    await audioHandler.ProcessAudio(id, dir, token, log);
                    break;
                case Mode.All:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "This mode should have been replaced before processing");
                default:
                    throw new NotImplementedException();
            }
        }

       
    }
}