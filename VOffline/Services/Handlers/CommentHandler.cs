using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers
{
    public class CommentHandler : HandlerBase<Comment>
    {
        private readonly AttachmentProcessor attachmentProcessor;

        public CommentHandler(FilesystemTools filesystemTools, AttachmentProcessor attachmentProcessor) : base(filesystemTools)
        {
            this.attachmentProcessor = attachmentProcessor;
        }

        public override async Task ProcessInternal(Comment comment, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var attachmentTasks = comment.Attachments.Select((a, i) => attachmentProcessor.ProcessAttachment(a, i+1, workDir, token, log));
            await Task.WhenAll(attachmentTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(Comment comment, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, $"{comment.Date.Value:s} {comment.Id}", CreateMode.OverwriteExisting);
    }
}