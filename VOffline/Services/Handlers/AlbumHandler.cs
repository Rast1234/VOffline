using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers
{
    public class AlbumHandler : HandlerBase<AlbumWithPhoto>
    {
        private readonly AttachmentProcessor attachmentProcessor;

        public AlbumHandler(FilesystemTools filesystemTools, AttachmentProcessor attachmentProcessor) : base(filesystemTools)
        {
            this.attachmentProcessor = attachmentProcessor;
        }

        public override async Task ProcessInternal(AlbumWithPhoto albumWithPhoto, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(albumWithPhoto.Album.Description))
            {
                var text = filesystemTools.CreateFile(workDir, $"__description.txt", CreateMode.MergeWithExisting);
                File.WriteAllText(text.FullName, albumWithPhoto.Album.Description);
            }

            if (albumWithPhoto.Album.ThumbId != null)
            {
                var cover = albumWithPhoto.Photo.FirstOrDefault(x => x.Id == albumWithPhoto.Album.ThumbId);
                if (cover != null)
                {
                    await attachmentProcessor.ProcessAttachment(cover, -1, workDir, token, log);
                }
            }

            var attachmentTasks = albumWithPhoto.Photo.Select((a, i) => attachmentProcessor.ProcessAttachment(a, i, workDir, token, log));
            await Task.WhenAll(attachmentTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(AlbumWithPhoto albumWithPhoto, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, $"{albumWithPhoto.Album.Title}", CreateMode.MergeWithExisting);
    }
}