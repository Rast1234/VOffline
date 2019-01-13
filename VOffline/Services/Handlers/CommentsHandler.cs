using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;
using VOffline.Services.VkNetHacks;

namespace VOffline.Services.Handlers
{
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
}