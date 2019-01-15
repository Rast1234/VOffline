using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VkNet.Utils;
using VOffline.Models.Storage;

namespace VOffline.Services.Vk
{
    public class VkApiUtils
    {
        private readonly VkApi vkApi;

        public VkApiUtils(VkApi vkApi)
        {
            this.vkApi = vkApi;
        }

        public async Task<long> ResolveId(string target)
        {
            // any community type with id, eg. club123 or event123
            var communityMatch = CommunityPattern.Match(target);
            if (communityMatch.Success)
            {
                return -1 * Int64.Parse(communityMatch.Groups[2].Value);
            }

            // any user eg. id123
            var personalMatch = PersonalPattern.Match(target);
            if (personalMatch.Success)
            {
                return Int64.Parse(personalMatch.Groups[2].Value);
            }

            // any id eg. 123 or -123
            var digitalMatch = DigitalPattern.Match(target);
            if (digitalMatch.Success)
            {
                return Int64.Parse(digitalMatch.Groups[1].Value);
            }

            // any screen name
            var vkObj = await vkApi.Utils.ResolveScreenNameAsync(target);
            switch (vkObj.Type)
            {
                case VkObjectType.User:
                    return vkObj.Id.Value;
                case VkObjectType.Group:
                    return -1 * vkObj.Id.Value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<string> GetName(long id)
        {
            if (id >= 0)
            {
                var users = await vkApi.Users.GetAsync(new []{id}, ProfileFields.All);
                var user = users[0];
                return String.Join(" ", user.LastName, user.FirstName, GroupOrEmpty(" - ", user.Id.ToString(), user.ScreenName, user.Domain));
            }
            var groups = await vkApi.Groups.GetByIdAsync(null, (-1*id).ToString(), GroupsFields.All);
            var group = groups[0];
            return String.Join(" ", group.Name, GroupOrEmpty(" - ", group.Id.ToString(), group.ScreenName, group.Type.ToString()));
        }

        public PageGetter<PhotoAlbum> PhotoAlbums(long id) => async (count, offset) => await vkApi.Photo.GetAlbumsAsync(new PhotoGetAlbumsParams()
        {
            OwnerId = id,
            NeedCovers = true,
            NeedSystem = true,
            Count = (uint)count,
            Offset = (uint)offset
        });

        public PageGetter<Photo> Photos(long id) => async (count, offset) => await vkApi.Photo.GetAllAsync(new PhotoGetAllParams()
        {
            OwnerId = id,
            Extended = true,
            SkipHidden = false,
            Count = (ulong)count,
            Offset = (ulong)offset
        });

        public PageGetter<Photo> PhotosInAlbum(long id, PhotoAlbumType albumIdType) => async (count, offset) => await vkApi.Photo.GetAsync(new PhotoGetParams()
        {
            OwnerId = id,
            AlbumId = albumIdType,
            Extended = true,
            PhotoSizes = true,
            Count = (ulong)count,
            Offset = (ulong)offset
        });

        public PageGetter<Post> Posts(long id) => async (count, offset) =>
        {
            var response = await vkApi.Wall.GetAsync(new WallGetParams
            {
                OwnerId = id,
                Count = (ulong)count,
                Offset = (ulong)offset
            });
            return new VkCollection<Post>(response.TotalCount, response.WallPosts);
        };

        public PageGetter<AudioPlaylist> Playlists(long id) => async (count, offset) => await vkApi.Audio.GetPlaylistsAsync(id, (uint)count, (uint)offset);

        public PageGetter<Audio> Audios(long id) => async (count, offset) => await vkApi.Audio.GetAsync(new AudioGetParams()
        {
            OwnerId = id,
            Count = (long)count,
            Offset = (long)offset
        });

        public PageGetter<Audio> AudiosInPlaylist(AudioPlaylist playlist) => async (count, offset) => await vkApi.Audio.GetAsync(new AudioGetParams()
        {
            OwnerId = playlist.OwnerId.Value,
            AlbumId = playlist.Id.Value,
            Count = (long)count,
            Offset = (long)offset
        });

        public PageGetter<Comment> Comments(Post post) => async (count, offset) => await vkApi.Wall.GetCommentsAsync(new WallGetCommentsParams()
        {
            Count = (uint)count,
            Offset = (uint)offset,
            OwnerId = post.OwnerId.Value,
            PostId = post.Id.Value,
        });

        public async Task<IReadOnlyList<T>> GetAllPagesAsync<T>(PageGetter<T> pageGetter, decimal pageSize, CancellationToken token, ILog log, bool throwIfCountMismatch=false)
        {
            var firstPage = await pageGetter(pageSize, 0);
            var total = firstPage.TotalCount;
            log.Debug($"{0}/{total}, {firstPage.Count} items");

            var result = new HashSet<T>((int)total);
            result.AddUnique(firstPage, log);

            var remainingPages = total / pageSize;
            var pageTasks = Enumerable.Range(1, (int)remainingPages)
                .Select(pageNumber => (ulong)pageNumber * pageSize)
                .Select(async offset =>
                {
                    var page = await pageGetter(pageSize, offset);
                    log.Debug($"{0}/{total}, {page.Count} items");
                    token.ThrowIfCancellationRequested();
                    return page;
                });
            var pages = await Task.WhenAll(pageTasks);
            foreach (var page in pages)
            {
                result.AddUnique(page, log);
            }

            if ((int)total != result.Count)
            {
                var message = $"Expected {total} items, got {result.Count}. Maybe they were created/deleted, or it's VK bugs again.";
                log.Warn(message);
                if (throwIfCountMismatch)
                {
                    throw new InvalidOperationException(message);
                }
            }

            return result.ToList();
        }

        public async Task<PlaylistWithAudio> ExpandPlaylist(AudioPlaylist playlist, CancellationToken token, ILog log)
        {
            var audios = await GetAllPagesAsync(AudiosInPlaylist(playlist), long.MaxValue, token, log, true);
            log.Debug($"Expanded playlist {playlist.Title}: {audios.Count} audios");
            return new PlaylistWithAudio(playlist, audios);
        }

        public async Task<int> GetPhotoAlbumsSimpleCountAsync(long id, CancellationToken token, ILog log)
        {
            throw new NotImplementedException();
            var errors = new Lazy<List<Exception>>();
            try
            {
                return await vkApi.Photo.GetAlbumsCountAsync(id, null);
            }
            catch (Exception e)
            {
                errors.Value.Add(e);

            }
            token.ThrowIfCancellationRequested();
            try
            {
                return await vkApi.Photo.GetAlbumsCountAsync(null, id);
            }
            catch (Exception e)
            {
                errors.Value.Add(e);

            }
            throw new Exception(string.Join("\n---------------------------------\n", errors.Value.Select(x => x.ToString())));
        }

        public async Task<AlbumWithPhoto> ExpandAlbum(Album album, CancellationToken token, ILog log)
        {
            var photos = await GetAllPagesAsync(PhotosInAlbum(album.OwnerId.Value, GetAlbumType(album.Id.Value)), 1000, token, log, true);
            return new AlbumWithPhoto(album, photos);
        }

        public async Task<AlbumWithPhoto> ExpandAlbum(PhotoAlbum album, CancellationToken token, ILog log)
        {
            var photos = await GetAllPagesAsync(PhotosInAlbum(album.OwnerId.Value, GetAlbumType(album.Id)), 1000, token, log, true);
            return new AlbumWithPhoto(album, photos);
        }

        private static PhotoAlbumType GetAlbumType(long albumId)
        {
            switch (albumId)
            {
                case -6:
                    return PhotoAlbumType.Profile;
                case -7:
                    return PhotoAlbumType.Wall;
                case -15:
                    return PhotoAlbumType.Saved;
                default:
                    return PhotoAlbumType.Id(albumId);
            }
        }

        private static string GroupOrEmpty(string separator, params string[] parts)
        {
            var all = String.Join(separator, parts);
            return String.IsNullOrEmpty(all)
                ? String.Empty
                : $"({all})";
        }

        private static readonly Regex CommunityPattern = new Regex(@"^(public|club|event)(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PersonalPattern = new Regex(@"^(id)(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DigitalPattern = new Regex(@"^(-?\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public delegate Task<VkCollection<T>> PageGetter<T>(decimal count, decimal offset);
}