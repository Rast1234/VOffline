using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model;
using VOffline.Models.Storage;

namespace VOffline.Services.Handlers.Attachments
{
    class AudioCoverAttachmentHandler : AttachmentHandlerBase<AudioCover>
    {
        public override Task<IEnumerable<object>> Process(OrderedAttachment<AudioCover> attachment, CancellationToken token, ILog log)
        {
            var cover = attachment.Data;
            var best = cover.Photo600
                       ?? cover.Photo300
                       ?? cover.Photo270
                       ?? cover.Photo135
                       ?? cover.Photo68
                       ?? cover.Photo34;
            var image = best == null
                ? null
                : new Uri(best);

            // TODO: i guess it's always jpeg?
            var ext = Path.HasExtension(image?.AbsoluteUri) ? Path.GetExtension(image?.AbsoluteUri) : ".jpg";
            if (image == null)
            {
                return Task.FromResult(Nothing);
            }
            var filename = $"__cover{ext}";
            var result = new List<Download>(1)
            {
                new Download(image, attachment.WorkingDir, filename)
            };
            return Task.FromResult(result.Cast<object>());
        }
    }
}