using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;

namespace VOffline.Services.Handlers.Attachments
{
    class VideoAttachmentHandler : AttachmentHandlerBase<Video>
    {
        public override Task<IEnumerable<object>> Process(OrderedAttachment<Video> attachment, CancellationToken token, ILog log)
        {
            return Task.FromResult(ConvertToDownloads(attachment, log));
        }

        private static IEnumerable<object> ConvertToDownloads(OrderedAttachment<Video> attachment, ILog log)
        {
            var video = attachment.Data;
            var vkUrl = video.Files?.Mp4_1080
                        ?? video.Files?.Mp4_720
                        ?? video.Files?.Mp4_480
                        ?? video.Files?.Mp4_360
                        ?? video.Files?.Mp4_240;
            if (vkUrl != null)
            {
                yield return new Download(vkUrl, attachment.WorkingDir, video.Title);
            }
            else if (video.Files?.External != null)
            {
                log.Warn($"Video {video.Id} [{video.Title}] is external");
                yield return new Download(video.Files.External, attachment.WorkingDir, video.Title);
            }
            else
            {
                log.Warn($"Video {video.Id} [{video.Title}] has no links");
            }
        }
    }
}