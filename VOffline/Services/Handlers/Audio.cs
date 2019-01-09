using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using RestSharp;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using VOffline.Models.Storage;
using VOffline.Services.Vk;
using RestClient = RestSharp.RestClient;

namespace VOffline.Services.Handlers
{
    public class AudioHandler
    {
        private readonly VkApi vkApi;

        public AudioHandler(VkApi vkApi)
        {
            this.vkApi = vkApi;
        }


        public async Task ProcessAudio(long id, Storage storage, CancellationToken token, ILog log)
        {
            var allPlaylists = await GetAllPlaylists(id, token, log);
            var throttler = new Throttler();
            foreach (var playlist in allPlaylists)
            {
                var playlistStorage = storage.Descend(playlist.Name, true);
                var downloadTasks = playlist.Tracks
                    .Select(async track => await Download(track, playlistStorage, token, log))
                    .ToArray();
                await throttler.ProcessWithThrottling(downloadTasks, 3, token);
            }
        }

        private async Task Download(Track track, Storage playlistStorage, CancellationToken token, ILog log)
        {
            return;
            var trackName = string.Join(" - ", new[] { track.Artist, track.Name }.Where(x => !string.IsNullOrEmpty(x)));
            if (track.Url != null)
            {
                var content = await Retrier.Retry(async () =>
                {
                    var client = new RestClient($"{track.Url.Scheme}://{track.Url.Authority}");
                    var response = await client.ExecuteGetTaskAsync(new RestRequest(track.Url.PathAndQuery), token);
                    response.ThrowIfSomethingWrong();

                    return response.RawBytes;
                }, 3, TimeSpan.FromSeconds(5), token, log);

                var file = playlistStorage.GetFile($"{trackName}.mp3").FullName;
                log.Debug($"Saving {file}");
                await File.WriteAllBytesAsync(file, content, token);
            }
            else
            {
                playlistStorage.GetFile($"{trackName}.mp3.deleted");
            }

            if (track.Lyrics != null)
            {
                var lyricsFile = playlistStorage.GetFile($"{trackName}.txt").FullName;
                await File.WriteAllTextAsync(lyricsFile, track.Lyrics, token);
            }
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
                    var lyrics = await GetLyrics(audio, log);
                    return new Track(audio, lyrics);
                });
                var tracks = await Task.WhenAll(audioTasks);
                return new Playlist(playlist, tracks);
            });
            return await Task.WhenAll(playlistTasks);
        }

        public async Task<IReadOnlyList<Track>> GetTracks(long id, HashSet<long> tracksInPlaylists, CancellationToken token, ILog log)
        {
            // TODO: handle paging if api returns partial result
            log.Debug("Getting all tracks not in playlists");
            var vkAudios = await vkApi.Audio.GetAsync(new AudioGetParams()
            {
                OwnerId = id
            });
            ThrowIfCountMismatch(vkAudios.TotalCount, vkAudios.Count);
            var trackTasks = vkAudios
                .Where(t => !tracksInPlaylists.Contains(t.Id.Value))
                .Select(async audio =>
                {
                    // TODO: filter out extra tracks here
                    var lyrics = await GetLyrics(audio, log);
                    token.ThrowIfCancellationRequested();
                    return new Track(audio, lyrics);
                });
            return await Task.WhenAll(trackTasks);
        }

        private async Task<IReadOnlyList<AudioPlaylist>> GetAllVkPlaylists(long id, CancellationToken token, ILog log)
        {
            var pageSize = 200;
            var playlistResponse = await vkApi.Audio.GetPlaylistsAsync(id, (uint)pageSize);

            var total = (int)playlistResponse.TotalCount;
            var result = new List<AudioPlaylist>(total);
            result.AddRange(playlistResponse);

            var remainingPages = total / pageSize;
            var pageTasks = Enumerable.Range(1, remainingPages)
                .Select(pageNumber => pageNumber * pageSize)
                .Select(async offset =>
                {
                    log.Debug($"Playlists at offset {offset}");
                    var page = await vkApi.Audio.GetPlaylistsAsync(id, (uint)pageSize, (uint)offset);
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
            log.Debug($"Expanding {playlist.Title}");
            var vkAudios = await vkApi.Audio.GetAsync(new AudioGetParams()
            {
                AlbumId = playlist.Id,
                OwnerId = playlist.OwnerId,
            });
            ThrowIfCountMismatch(vkAudios.TotalCount, vkAudios.Count);
            return vkAudios;
        }

        private async Task<Lyrics> GetLyrics(Audio audio, ILog log)
        {
            if (audio.LyricsId == null)
            {
                return null;
            }
            log.Debug($"Lyrics for {audio.Title}");
            return await vkApi.Audio.GetLyricsAsync(audio.LyricsId.Value);
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
