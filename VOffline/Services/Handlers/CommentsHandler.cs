using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers
{
    public class CommentsHandler : HandlerBase<Post>
    {
        private readonly VkApiUtils vkApiUtils;
        private readonly IHandler<Comment> commentHandler;

        public CommentsHandler(VkApiUtils vkApiUtils, FilesystemTools filesystemTools, IHandler<Comment> commentHandler) : base(filesystemTools)
        {
            this.vkApiUtils = vkApiUtils;
            this.commentHandler = commentHandler;
        }

        public override async Task ProcessInternal(Post post, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allComments = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Comments(post), 100, token, log);
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

        public override DirectoryInfo GetWorkingDirectory(Post post, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Comments", CreateMode.OverwriteExisting);
    }
}