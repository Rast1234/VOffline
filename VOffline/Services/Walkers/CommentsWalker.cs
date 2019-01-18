using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Walkers
{
    public class CommentsWalker : WalkerBase<PostComments>
    {
        private readonly VkApiUtils vkApiUtils;
        private readonly IWalker<Comment> commentWalker;

        public CommentsWalker(VkApiUtils vkApiUtils, FilesystemTools filesystemTools, IWalker<Comment> commentWalker) : base(filesystemTools)
        {
            this.vkApiUtils = vkApiUtils;
            this.commentWalker = commentWalker;
        }

        public override async Task ProcessInternal(PostComments postComments, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allComments = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Comments(postComments.Post), 100, token, log);
            log.Debug($"Post {postComments.Post.Id} has {allComments.Count} comments");
            var byDate = allComments
                .OrderBy(c => c.Date)
                .ToList();
            await byDate.SaveHumanReadableText(filesystemTools, workDir, token, log);
            var commentTasks = byDate
                .Where(c => c.Attachments.Count > 0)
                .Select(c => commentWalker.Process(c, workDir, token, log));
            await Task.WhenAll(commentTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(PostComments postComments, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Comments", CreateMode.OverwriteExisting);
    }
}