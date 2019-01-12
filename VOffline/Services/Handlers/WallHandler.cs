using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet;
using VkNet.Model;
using VkNet.Model.RequestParams;
using VOffline.Services.Storage;
using VOffline.Services.Vk;
using VOffline.Services.VkNetHacks;

namespace VOffline.Services.Handlers
{
    public class WallHandler : HandlerBase
    {
        private readonly VkApi vkApi;
        private readonly AttachmentProcessor attachmentProcessor;
        private readonly long id;

        public WallHandler(long id, VkApi vkApi, FilesystemTools filesystemTools, AttachmentProcessor attachmentProcessor):base(filesystemTools)
        {
            this.id = id;
            this.vkApi = vkApi;
            this.attachmentProcessor = attachmentProcessor;
        }

        public override async Task ProcessInternal(DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allPosts = await vkApi.Wall.GetAllAsync(id, token, log);
            log.Debug($"Wall has {allPosts.Count} posts");
            var allTasks = allPosts
                .OrderBy(x => x.Date)
                .Select(p => new PostHandler(p, filesystemTools, attachmentProcessor).Process(workDir, token, log));
            await Task.WhenAll(allTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Wall", CreateMode.MergeWithExisting);
    }
}