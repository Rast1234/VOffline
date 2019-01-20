using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers.Categories
{
    public class WallCategoryHandler : CategoryHandlerBase<WallCategory>
    {
        private readonly VkApiUtils vkApiUtils;

        public WallCategoryHandler(VkApiUtils vkApiUtils, FileSystemTools fileSystemTools):base(fileSystemTools)
        {
            this.vkApiUtils = vkApiUtils;
        }

        public override async Task<IEnumerable<object>> ProcessInternal(WallCategory wall, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allPosts = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Posts(wall.OwnerId), 100, token, log);
            log.Debug($"Wall has {allPosts.Count} posts");
            return allPosts
                .OrderBy(x => x.Date)
                .Select(p => new Nested<Post>(p, workDir, GetPostDirName(p)));
        }

        private string GetPostDirName(Post post)
        {
            var dateMaybe = post.Date != null
                ? $"{post.Date.Value:s}"
                : "no_date";
            return $"{dateMaybe} {post.Id}";
        }
    }
}