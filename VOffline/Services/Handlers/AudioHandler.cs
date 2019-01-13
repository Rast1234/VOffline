using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using Nito.AsyncEx;
using RestSharp;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;
using VOffline.Services.VkNetHacks;
using RestClient = RestSharp.RestClient;

namespace VOffline.Services.Handlers
{
    public class AudioHandler : HandlerBase<long>
    {
        private readonly VkApi vkApi;
        private readonly PlaylistHandler playlistHandler;

        public AudioHandler(VkApi vkApi, FilesystemTools filesystemTools, PlaylistHandler playlistHandler) : base(filesystemTools)
        {
            this.vkApi = vkApi;
            this.playlistHandler = playlistHandler;
        }

        public override async Task ProcessInternal(long id, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var vkPlaylists = await vkApi.Audio.GetAllPlaylistsAsync(id, token, log);
            log.Debug($"Audio: {vkPlaylists.Count} playlists");
            var expandTasks = vkPlaylists.Select(p => vkApi.Audio.ExpandPlaylist(p, token, log));
            var playlists = await Task.WhenAll(expandTasks);
            log.Debug($"Audio: {playlists.Sum(p => p.Audio.Count)} in {playlists.Length} playlists");

            var allAudios = await vkApi.Audio.GetAllAudios(id, token, log);
            var audioInPlaylists = playlists
                .SelectMany(p => p.Audio.Select(t => t.Id))
                .ToHashSet();
            var uniqueAudios = allAudios
                .Where(a => !audioInPlaylists.Contains(a.Id.Value))
                .ToList();
            var defaultPlaylist = new PlaylistWithAudio(uniqueAudios);
            log.Debug($"Audio: {allAudios.Count} total, {defaultPlaylist.Audio.Count} unique in default playlist");

            var allPlaylists = playlists.ToList();
            allPlaylists.Add(defaultPlaylist);

            var allTasks = allPlaylists
                .OrderBy(p => p.Playlist.CreateTime)
                .Select(p => playlistHandler.Process(p, workDir, token, log));
            await Task.WhenAll(allTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(long id, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Audio", CreateMode.MergeWithExisting);
    }
}
