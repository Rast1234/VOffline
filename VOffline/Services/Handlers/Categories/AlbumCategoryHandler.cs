using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers.Categories
{
    public class AlbumCategoryHandler : CategoryHandlerBase<AlbumWithPhoto>
    {
        public AlbumCategoryHandler(FileSystemTools fileSystemTools) : base(fileSystemTools)
        {
        }

        public override async Task<IEnumerable<object>> ProcessInternal(AlbumWithPhoto albumWithPhoto, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(albumWithPhoto.Album.Description))
            {
                var text = fileSystemTools.CreateFile(workDir, $"__description.txt", CreateMode.OverwriteExisting);
                await File.WriteAllTextAsync(text.FullName, albumWithPhoto.Album.Description, token);
            }

            return GetAttachments(albumWithPhoto, workDir);
        }

        private IEnumerable<OrderedAttachment<Photo>> GetAttachments(AlbumWithPhoto albumWithPhoto, DirectoryInfo workDir)
        {
            if (albumWithPhoto.Album.ThumbId != null)
            {
                var cover = albumWithPhoto.Photo.FirstOrDefault(x => x.Id == albumWithPhoto.Album.ThumbId);
                if (cover != null)
                {
                    yield return new OrderedAttachment<Photo>(cover, -1, workDir);
                }
            }

            var i = 1;
            foreach (var photo in albumWithPhoto.Photo)
            {
                yield return new OrderedAttachment<Photo>(photo, i, workDir);
                i++;
            }
        }
    }
}