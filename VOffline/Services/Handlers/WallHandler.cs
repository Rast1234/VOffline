using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers
{
    public class WallHandler : HandlerBase<long>
    {
        private readonly VkApiUtils vkApiUtils;
        private readonly IHandler<Post> postHandler;

        public WallHandler(VkApiUtils vkApiUtils, FilesystemTools filesystemTools, IHandler<Post> postHandler):base(filesystemTools)
        {
            this.vkApiUtils = vkApiUtils;
            this.postHandler = postHandler;
        }

        public override async Task ProcessInternal(long id, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allPosts = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Posts(id), 100, token, log);
            log.Debug($"Wall has {allPosts.Count} posts");
            var allTasks = allPosts
                .OrderBy(x => x.Date)
                .Select(p => postHandler.Process(p, workDir, token, log));
            await Task.WhenAll(allTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(long id, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Wall", CreateMode.OverwriteExisting);
    }
}