using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Walkers
{
    public class PhotoWalker : WalkerBase<PhotoCategory>
    {
        private readonly VkApiUtils vkApiUtils;
        private readonly IWalker<AlbumWithPhoto> albumWalker;

        public PhotoWalker(VkApiUtils vkApiUtils, FilesystemTools filesystemTools, IWalker<AlbumWithPhoto> albumWalker) : base(filesystemTools)
        {
            this.vkApiUtils = vkApiUtils;
            this.albumWalker = albumWalker;
        }

        public override async Task ProcessInternal(PhotoCategory photos, DirectoryInfo workDir, CancellationToken token, ILog log)
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

            var allTasks = allAlbums
                .OrderBy(p => p.Album.Created)
                .Select(p => albumWalker.Process(p, workDir, token, log));
            await Task.WhenAll(allTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(PhotoCategory photos, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Photo", CreateMode.OverwriteExisting);
    }
}