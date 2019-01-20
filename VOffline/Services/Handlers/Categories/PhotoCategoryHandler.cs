using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers.Categories
{
    public class PhotoCategoryHandler : CategoryHandlerBase<PhotoCategory>
    {
        private readonly VkApiUtils vkApiUtils;

        public PhotoCategoryHandler(VkApiUtils vkApiUtils, FileSystemTools fileSystemTools) : base(fileSystemTools)
        {
            this.vkApiUtils = vkApiUtils;
        }

        public override async Task<IEnumerable<object>> ProcessInternal(PhotoCategory photos, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var vkAlbums = await vkApiUtils.GetAllPagesAsync(vkApiUtils.PhotoAlbums(photos.OwnerId), int.MaxValue, token, log);
            var expandTasks = vkAlbums.Select(album => vkApiUtils.ExpandAlbum(album, token, log));
            var albums = await Task.WhenAll(expandTasks);

            var allPhotos = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Photos(photos.OwnerId), 100, token, log);
            var photosInAlbums = albums
                .SelectMany(awp => awp.Photo.Select(photo => photo.Id.Value))
                .ToHashSet();
            var uniquePhotos = allPhotos
                .Where(photo => !photosInAlbums.Contains(photo.Id.Value))
                .ToList();
            var defaultAlbum = new AlbumWithPhoto(uniquePhotos);

            var allAlbums = albums.ToList();
            if (defaultAlbum.Photo.Any())
            {
                allAlbums.Add(defaultAlbum);
            }

            return allAlbums
                .OrderBy(a => a.Album.Created)
                .Select(a => new Nested<AlbumWithPhoto>(a, workDir, $"{a.Album.Title}"));
        }
    }
}