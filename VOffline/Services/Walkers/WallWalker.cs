using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Walkers
{
    public class WallWalker : WalkerBase<WallCategory>
    {
        private readonly VkApiUtils vkApiUtils;
        private readonly IWalker<Post> postWalker;

        public WallWalker(VkApiUtils vkApiUtils, FilesystemTools filesystemTools, IWalker<Post> postWalker):base(filesystemTools)
        {
            this.vkApiUtils = vkApiUtils;
            this.postWalker = postWalker;
        }

        public override async Task ProcessInternal(WallCategory wall, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allPosts = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Posts(wall.OwnerId), 100, token, log);
            log.Debug($"Wall has {allPosts.Count} posts");
            var allTasks = allPosts
                .OrderBy(x => x.Date)
                .Select(p => postWalker.Process(p, workDir, token, log));
            await Task.WhenAll(allTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(WallCategory wall, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Wall", CreateMode.OverwriteExisting);
    }
}