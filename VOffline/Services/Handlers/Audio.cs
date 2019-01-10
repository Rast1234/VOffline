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
using RestClient = RestSharp.RestClient;

namespace VOffline.Services.Handlers
{
    public class AudioHandler
    {
        private readonly VkApi vkApi;
        private readonly FilesystemTools filesystemTools;
        private readonly DownloadQueueProvider downloadQueueProvider;

        public AudioHandler(VkApi vkApi, FilesystemTools filesystemTools, DownloadQueueProvider downloadQueueProvider)
        {
            this.vkApi = vkApi;
            this.filesystemTools = filesystemTools;
            this.downloadQueueProvider = downloadQueueProvider;
        }


        public async Task ProcessAudio(long id, DirectoryInfo dir, CancellationToken token, ILog log)
        {
            // TODO: continuation/recovery? at least on album level? maybe create and detect some 'completeness' file?
            var allPlaylists = await GetAllPlaylists(id, token, log);
            var allDownloadTasks = allPlaylists
                .SelectMany(p => p.ToDownloads(filesystemTools, dir))
                .Select(d => downloadQueueProvider.Pending.EnqueueAsync(d, token));
            await Task.WhenAll(allDownloadTasks);
        }

        private async Task<IReadOnlyList<Playlist>> GetAllPlaylists(long id, CancellationToken token, ILog log)
        {
            var playlists = await GetPlaylists(id, token, log);
            token.ThrowIfCancellationRequested();
            var tracksInPlaylists = playlists
                .SelectMany(p => p.Tracks.Select(t => t.Id))
                .ToHashSet();
            var uniqueTracks = await GetTracks(id, tracksInPlaylists, token, log);

            log.Info($"Found {uniqueTracks.Count} tracks and {playlists.Sum(x => x.Tracks.Count)} in {playlists.Count} playlists");

            var defaultPlaylist = new Playlist(uniqueTracks);
            var allPlaylists = playlists.ToList();
            allPlaylists.Add(defaultPlaylist);

            return allPlaylists;
        }

        public async Task<IReadOnlyList<Playlist>> GetPlaylists(long id, CancellationToken token, ILog log)
        {
            var vkPlaylists = await GetAllVkPlaylists(id, token, log);
            var playlistTasks = vkPlaylists.Select(async playlist =>
            {
                var vkAudios = await ExpandPlaylist(playlist, log);
                var audioTasks = vkAudios.Select(async audio =>
                {
                    token.ThrowIfCancellationRequested();
                    return new Track(audio);
                });
                var tracks = await Task.WhenAll(audioTasks);
                return new Playlist(playlist, tracks);
            });
            return await Task.WhenAll(playlistTasks);
        }

        public async Task<IReadOnlyList<Track>> GetTracks(long id, HashSet<long> tracksInPlaylists, CancellationToken token, ILog log)
        {
            // TODO: handle paging if api returns partial result
            var vkAudios = await vkApi.Audio.GetAsync(new AudioGetParams()
            {
                OwnerId = id
            });
            log.Debug($"Got {vkAudios.Count} tracks not in playlists for {id}");
            ThrowIfCountMismatch(vkAudios.TotalCount, vkAudios.Count);
            var trackTasks = vkAudios
                .Where(t => !tracksInPlaylists.Contains(t.Id.Value))
                .Select(async audio =>
                {
                    token.ThrowIfCancellationRequested();
                    return new Track(audio);
                });
            return await Task.WhenAll(trackTasks);
        }

        private async Task<IReadOnlyList<AudioPlaylist>> GetAllVkPlaylists(long id, CancellationToken token, ILog log)
        {
            var pageSize = 200;
            var playlistResponse = await vkApi.Audio.GetPlaylistsAsync(id, (uint)pageSize);
            log.Debug($"Got {playlistResponse.Count}/{playlistResponse.TotalCount} tracks in playlist {id}");

            var total = (int)playlistResponse.TotalCount;
            var result = new List<AudioPlaylist>(total);
            result.AddRange(playlistResponse);

            var remainingPages = total / pageSize;
            var pageTasks = Enumerable.Range(1, remainingPages)
                .Select(pageNumber => pageNumber * pageSize)
                .Select(async offset =>
                {
                    //log.Debug($"Playlists at offset {offset}");
                    var page = await vkApi.Audio.GetPlaylistsAsync(id, (uint)pageSize, (uint)offset);
                    log.Debug($"Got {page.Count}/{playlistResponse.TotalCount} tracks in playlist {id} at offset {offset}");
                    token.ThrowIfCancellationRequested();  // not sure if this is needed here
                    return page;
                });
            var pages = await Task.WhenAll(pageTasks);
            foreach (var page in pages)
            {
                result.AddRange(page);
            }
            ThrowIfCountMismatch(total, result.Count);
            return result;
        }

        private async Task<IReadOnlyList<Audio>> ExpandPlaylist(AudioPlaylist playlist, ILog log)
        {
            
            var vkAudios = await vkApi.Audio.GetAsync(new AudioGetParams()
            {
                AlbumId = playlist.Id,
                OwnerId = playlist.OwnerId,
            });
            log.Debug($"Expanded {playlist.Title}: {vkAudios.TotalCount}");
            ThrowIfCountMismatch(vkAudios.TotalCount, vkAudios.Count);
            return vkAudios;
        }

        private async Task<Lyrics> GetLyrics(long lyricsId, ILog log)
        {
            var lyrics = await vkApi.Audio.GetLyricsAsync(lyricsId);
            log.Debug($"Got lyrics for {lyricsId}");
            return lyrics;
        }

        private static void ThrowIfCountMismatch(decimal expectedTotal, decimal resultCount)
        {
            if (resultCount != expectedTotal)
            {
                throw new InvalidOperationException($"Expected {expectedTotal} items, got {resultCount}. Maybe they were created/deleted, try again.");
            }
        }
    }
}
