using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet;
using VOffline.Services.Storage;
using VOffline.Services.Vk;
using VOffline.Services.VkNetHacks;

namespace VOffline.Services.Handlers
{
    public class WallHandler : HandlerBase<long>
    {
        private readonly VkApi vkApi;
        private readonly PostHandler postHandler;

        public WallHandler(VkApi vkApi, FilesystemTools filesystemTools, PostHandler postHandler):base(filesystemTools)
        {
            this.vkApi = vkApi;
            this.postHandler = postHandler;
        }

        public override async Task ProcessInternal(long id, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allPosts = await vkApi.Wall.GetAllPostsAsync(id, token, log);
            log.Debug($"Wall has {allPosts.Count} posts");
            var allTasks = allPosts
                .OrderBy(x => x.Date)
                .Select(p => postHandler.Process(p, workDir, token, log));
            await Task.WhenAll(allTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(long id, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Wall", CreateMode.MergeWithExisting);
    }
}