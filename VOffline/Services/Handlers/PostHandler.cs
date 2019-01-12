using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers
{
    public class PostHandler : HandlerBase
    {
        private readonly Post post;
        private readonly AttachmentProcessor attachmentProcessor;

        public PostHandler(Post post, FilesystemTools filesystemTools, AttachmentProcessor attachmentProcessor) : base(filesystemTools)
        {
            this.post = post;
            this.attachmentProcessor = attachmentProcessor;
        }

        public override async Task ProcessInternal(DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(post.Text))
            {
                var postText = filesystemTools.CreateFile(workDir, $"text.txt", CreateMode.MergeWithExisting);
                File.WriteAllText(postText.FullName, post.Text);
            }

            var attachmentTasks = post.Attachments.Select((a, i) => attachmentProcessor.ProcessAttachment(a, i, workDir, token, log));
            await Task.WhenAll(attachmentTasks);

            // TODO: process comments
            //var queueTasks = ToDownloads(post, filesystemTools, workDir, log)
            //    .Select(d => downloadQueueProvider.Pending.EnqueueAsync(d, token));
            //await Task.WhenAll(queueTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, $"{post.Date.Value:s} {post.Id}", CreateMode.MergeWithExisting);
    }
}