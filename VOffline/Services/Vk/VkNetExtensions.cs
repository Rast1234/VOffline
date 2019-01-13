using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VOffline.Models.Storage;

namespace VOffline.Services.Vk
{
    public static class VkNetExtensions
    {
        public static async Task<IReadOnlyList<Post>> GetAllPostsAsync(this IWallCategory wall, long id, CancellationToken token, ILog log)
        {
            var pageSize = 100u;
            var wallResponse = await wall.GetAsync(new WallGetParams
            {
                OwnerId = id,
                Count = pageSize
            });
            log.Debug($"Wall posts [{id}]: {wallResponse.WallPosts.Count}, offset {0}/{wallResponse.TotalCount}");

            var total = wallResponse.TotalCount;
            var result = new HashSet<Post>((int) total);

            void AddToResult(IEnumerable<Post> posts)
            {
                foreach (var item in posts)
                {
                    if (!result.Add(item))
                    {
                        log.Warn($"Duplicate item [{item}]");
                    }
                }
            }

            AddToResult(wallResponse.WallPosts);

            var remainingPages = total / pageSize;
            var pageTasks = Enumerable.Range(1, (int) remainingPages)
                .Select(pageNumber => (ulong) pageNumber * pageSize)
                .Select(async offset =>
                {
                    var page = await wall.GetAsync(new WallGetParams
                    {
                        OwnerId = id,
                        Count = pageSize,
                        Offset = offset
                    });
                    log.Debug($"Wall posts [{id}]: {page.WallPosts.Count}, offset {offset}/{page.TotalCount}");
                    token.ThrowIfCancellationRequested();
                    return page;
                });
            var pages = await Task.WhenAll(pageTasks);
            foreach (var page in pages)
            {
                AddToResult(page.WallPosts);
            }

            if ((int) total != result.Count)
            {
                log.Warn($"Expected {total} items, got {result.Count}. Maybe they were created/deleted, or it's VK bugs again.");
            }

            return result
                .ToList();
        }

        public static async Task<IReadOnlyList<Comment>> GetAllCommentsAsync(this IWallCategory wall, long id, long postId, CancellationToken token, ILog log)
        {
            var pageSize = 100u;
            var commentResponse = await wall.GetCommentsAsync(new WallGetCommentsParams()
            {
                Count = pageSize,
                Offset = 0,
                OwnerId = id,
                PostId = postId,
            });

            log.Debug($"Post comments [{id} {postId}]: {commentResponse.Count}, offset {0}/{commentResponse.TotalCount}");

            var total = commentResponse.TotalCount;
            var result = new HashSet<Comment>((int)total);

            void AddToResult(IEnumerable<Comment> posts)
            {
                foreach (var item in posts)
                {
                    if (!result.Add(item))
                    {
                        log.Warn($"Duplicate item [{item}]");
                    }
                }
            }

            AddToResult(commentResponse);

            var remainingPages = total / pageSize;
            var pageTasks = Enumerable.Range(1, (int)remainingPages)
                .Select(pageNumber => (long)pageNumber * pageSize)
                .Select(async offset =>
                {
                    var page = await wall.GetCommentsAsync(new WallGetCommentsParams()
                    {
                        Count = pageSize,
                        Offset = offset,
                        OwnerId = id,
                        PostId = postId,
                    });
                    log.Debug($"Post comments [{id} {postId}]: {page.Count}, offset {0}/{page.TotalCount}");
                    token.ThrowIfCancellationRequested();
                    return page;
                });
            var pages = await Task.WhenAll(pageTasks);
            foreach (var page in pages)
            {
                AddToResult(page);
            }

            if ((int)total != result.Count)
            {
                log.Warn($"Expected {total} items, got {result.Count}. Maybe they were created/deleted, or it's VK bugs again.");
            }

            return result
                .ToList();
        }

        public static async Task<IReadOnlyList<AudioPlaylist>> GetAllPlaylistsAsync(this IAudioCategory audio, long id, CancellationToken token, ILog log)
        {
            var pageSize = 200u;
            var playlistResponse = await audio.GetPlaylistsAsync(id, pageSize);
            //log.Debug($"Got {playlistResponse.Count}/{playlistResponse.TotalCount} tracks in playlist {id}");
            log.Debug($"Playlists [{id}]: {playlistResponse.Count}, offset {0}/{playlistResponse.TotalCount}");

            var total = playlistResponse.TotalCount;
            var result = new HashSet<AudioPlaylist>((int) total);

            void AddToResult(IEnumerable<AudioPlaylist> playlists)
            {
                foreach (var item in playlists)
                {
                    if (!result.Add(item))
                    {
                        log.Warn($"Duplicate item [{item}]");
                    }
                }
            }

            AddToResult(playlistResponse);

            var remainingPages = total / pageSize;
            var pageTasks = Enumerable.Range(1, (int) remainingPages)
                .Select(pageNumber => (ulong) pageNumber * pageSize)
                .Select(async offset =>
                {
                    var page = await audio.GetPlaylistsAsync(id, pageSize, (uint) offset);
                    log.Debug($"Playlists [{id}]: {page.Count}, offset {offset}/{page.TotalCount}");
                    token.ThrowIfCancellationRequested();
                    return page;
                });
            var pages = await Task.WhenAll(pageTasks);
            foreach (var page in pages)
            {
                AddToResult(page);
            }

            if ((int) total != result.Count)
            {
                log.Warn($"Expected {total} items, got {result.Count}. Maybe they were created/deleted, or it's VK bugs again.");
            }

            return result
                .ToList();
        }

        public static async Task<PlaylistWithAudio> ExpandPlaylist(this IAudioCategory audio, AudioPlaylist playlist, CancellationToken token, ILog log)
        {
            var audios = await audio.GetAsync(new AudioGetParams
            {
                AlbumId = playlist.Id,
                OwnerId = playlist.OwnerId,
            });
            log.Debug($"Expanded playlist {playlist.Title}: {audios.TotalCount} audios");
            VkApiUtils.ThrowIfCountMismatch(audios.TotalCount, audios.Count);
            return new PlaylistWithAudio(playlist, audios);
        }

        public static async Task<IReadOnlyList<Audio>> GetAllAudios(this IAudioCategory audio, long id, CancellationToken token, ILog log)
        {
            // TODO: handle paging if api returns partial result
            var audios = await audio.GetAsync(new AudioGetParams()
            {
                OwnerId = id
            });
            log.Debug($"Audios [{id}]: {audios.Count}/{audios.TotalCount}");
            VkApiUtils.ThrowIfCountMismatch(audios.TotalCount, audios.Count);
            return audios;
        }
    }
}
