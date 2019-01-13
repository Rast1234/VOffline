using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.VkNetHacks;

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

            if (post.Comments.Count > 0)
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

    public class CommentsHandler : HandlerBase<Post>
    {
        private readonly VkApi vkApi;
        private readonly CommentHandler commentHandler;

        public CommentsHandler(VkApi vkApi, FilesystemTools filesystemTools, CommentHandler commentHandler) : base(filesystemTools)
        {
            this.vkApi = vkApi;
            this.commentHandler = commentHandler;
        }

        public override async Task ProcessInternal(Post post, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allComments = await vkApi.Wall.GetAllCommentsAsync(post.OwnerId.Value, post.Id.Value, token, log);
            log.Debug($"Post {post.Id} has {allComments.Count} comments");
            var byDate = allComments
                .OrderBy(c => c.Date)
                .ToList();
            await byDate.SaveHumanReadableText(filesystemTools, workDir, token, log);
            var commentTasks = byDate
                .Where(c => c.Attachments.Count > 0)
                .Select(c => commentHandler.Process(c, workDir, token, log));
            await Task.WhenAll(commentTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(Post post, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "comments", CreateMode.MergeWithExisting);
    }

    public class CommentHandler : HandlerBase<Comment>
    {
        private readonly AttachmentProcessor attachmentProcessor;

        public CommentHandler(FilesystemTools filesystemTools, AttachmentProcessor attachmentProcessor) : base(filesystemTools)
        {
            this.attachmentProcessor = attachmentProcessor;
        }

        public override async Task ProcessInternal(Comment comment, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var attachmentTasks = comment.Attachments.Select((a, i) => attachmentProcessor.ProcessAttachment(a, i, workDir, token, log));
            await Task.WhenAll(attachmentTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(Comment comment, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, $"{comment.Date.Value:s} {comment.Id}", CreateMode.MergeWithExisting);
    }
}