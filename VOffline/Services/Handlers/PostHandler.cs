using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers
{
    public class PostHandler : HandlerBase<Post>
    {
        private readonly AttachmentProcessor attachmentProcessor;
        private readonly CommentsHandler commentsHandler;

        public PostHandler(FilesystemTools filesystemTools, AttachmentProcessor attachmentProcessor, CommentsHandler commentsHandler) : base(filesystemTools)
        {
            this.attachmentProcessor = attachmentProcessor;
            this.commentsHandler = commentsHandler;
        }

        public override async Task ProcessInternal(Post post, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(post.Text))
            {
                var postText = filesystemTools.CreateFile(workDir, $"text.txt", CreateMode.MergeWithExisting);
                File.WriteAllText(postText.FullName, post.Text);
            }

            var attachmentTasks = post.Attachments.Select((a, i) => attachmentProcessor.ProcessAttachment(a, i, workDir, token, log));
            await Task.WhenAll(attachmentTasks);

            if (post.Comments?.Count > 0)
            {
                await commentsHandler.Process(post, workDir, token, log);
            }
            
            // recursively walk reposts
            var allRepostTasks = post.CopyHistory
                .OrderBy(x => x.Date)
                .Select(repost => Process(repost, workDir, token, log));
            await Task.WhenAll(allRepostTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(Post post, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, $"{post.Date.Value:s} {post.Id}", CreateMode.MergeWithExisting);
    }
}