using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using VkNet.Enums.SafetyEnums;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers.Attachments
{
    class PhotoAttachmentHandler : AttachmentHandlerBase<Photo>
    {
        private readonly FileSystemTools fileSystemTools;

        public PhotoAttachmentHandler(FileSystemTools fileSystemTools)
        {
            this.fileSystemTools = fileSystemTools;
        }

        public override async Task<IEnumerable<object>> Process(OrderedAttachment<Photo> attachment, CancellationToken token, ILog log)
        {
            await SaveText(attachment, fileSystemTools, token, log);
            var downloads = ConvertToDownloads(attachment, log);
            return downloads;
        }

        private static IEnumerable<Download> ConvertToDownloads(OrderedAttachment<Photo> attachment, ILog log)
        {
            var photo = attachment.Data;
            // TODO: not sure about ANYTHING here
            var url = photo.BigPhotoSrc
                      ?? photo.Photo2560
                      ?? photo.PhotoSrc
                      ?? photo.Photo1280
                      ?? photo.Photo807
                      ?? photo.Photo604
                      ?? photo.SmallPhotoSrc
                      ?? photo.Photo200
                      ?? photo.Photo130
                      ?? photo.Photo100
                      ?? photo.Photo75
                      ?? photo.Photo50
                      ?? photo.Sizes
                          .Select(s => (square: s.Width * s.Height, size: s))
                          .OrderByDescending(x => x.square)
                          .FirstOrDefault(s => s.square > 0) // width/height can be null
                          .size?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.W)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.Z)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.Y)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.X)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.R)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.Q)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.P)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.O)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.M)?.Url
                      ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.S)?.Url
                ;

            // TODO: i guess it's always jpeg?
            var ext = Path.HasExtension(url?.AbsoluteUri) ? Path.GetExtension(url?.AbsoluteUri) : ".jpg";
            if (url != null)
            {
                var file = $"{attachment.Number}_{photo.Id}{ext}";
                yield return new Download(url, attachment.WorkingDir, file);
            }
            else
            {
                log.Warn($"Photo with no url! {JsonConvert.SerializeObject(photo)}");
            }
        }

        private static async Task SaveText(OrderedAttachment<Photo> attachment, FileSystemTools fileSystemTools, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(attachment.Data.Text))
            {
                var textFile = fileSystemTools.CreateFile(attachment.WorkingDir, $"{attachment.Number} {attachment.Data.Id}.txt", CreateMode.OverwriteExisting);
                await File.WriteAllTextAsync(textFile.FullName, attachment.Data.Text, token);
            }
        }
    }
}