using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;

namespace VOffline.Services.Handlers.Attachments
{
    class DocumentAttachmentHandler : AttachmentHandlerBase<Document>
    {
        public override Task<IEnumerable<object>> Process(OrderedAttachment<Document> attachment, CancellationToken token, ILog log)
        {
            // TODO: looks like Title already has Extension
            var result = new List<Download>(1)
            {
                new Download(new Uri(attachment.Data.Uri), attachment.WorkingDir, $"{attachment.Number} {attachment.Data.Title}")
            };
            return Task.FromResult(result.Cast<object>());
        }
    }
}