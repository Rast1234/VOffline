using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers.Attachments
{
    class LinkAttachmentHandler : AttachmentHandlerBase<Link>
    {
        private readonly FileSystemTools fileSystemTools;

        public LinkAttachmentHandler(FileSystemTools fileSystemTools)
        {
            this.fileSystemTools = fileSystemTools;
        }

        public override async Task<IEnumerable<object>> Process(OrderedAttachment<Link> attachment, CancellationToken token, ILog log)
        {
            await SaveHumanReadableText(attachment, token, log);
            var result = new List<Download>(1)
            {
                new Download(attachment.Data.Uri, attachment.WorkingDir, $"{attachment.Number} {attachment.Data.Title}")
            };
            return result;
        }

        private async Task SaveHumanReadableText(OrderedAttachment<Link> attachment, CancellationToken token, ILog log)
        {
            var textFile = fileSystemTools.CreateFile(attachment.WorkingDir, $"{attachment.Number} {attachment.Data.Title}.txt", CreateMode.OverwriteExisting);
            await File.WriteAllTextAsync(textFile.FullName, Serialize(attachment.Data), token);
        }

        private string Serialize(Link link)
        {
            var sb = new StringBuilder();
            sb.AppendLine(link.Title);
            sb.AppendLine(link.Caption);
            sb.AppendLine(link.Description);
            sb.AppendLine($"{link.Uri}");
            return sb.ToString();
        }
    }
}