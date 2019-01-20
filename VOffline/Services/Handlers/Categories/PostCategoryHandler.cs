using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers.Categories
{
    public class PostCategoryHandler : CategoryHandlerBase<Post>
    {

        public PostCategoryHandler(FileSystemTools fileSystemTools) : base(fileSystemTools)
        {
        }

        public override async Task<IEnumerable<object>> ProcessInternal(Post post, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(post.Text))
            {
                var postText = fileSystemTools.CreateFile(workDir, $"text.txt", CreateMode.OverwriteExisting);
                await File.WriteAllTextAsync(postText.FullName, post.Text, token);
            }

            return GetContent(post, workDir);
        }

        private IEnumerable<object> GetContent(Post post, DirectoryInfo workDir)
        {
            var i = 1;
            foreach (var attachment in post.Attachments)
            {
                // holy moly, something went wrong
                var ordAttGeneric = typeof(OrderedAttachment<>);
                Type[] typeArgs = { attachment.Type };
                var ordAttType = ordAttGeneric.MakeGenericType(typeArgs);
                var result = Activator.CreateInstance(ordAttType, attachment.Instance, i, workDir);
                yield return result;
                i++;
            }

            if (post.Comments?.Count > 0)
            {
                yield return new Nested<PostComments>(new PostComments(post), workDir, "Comments");
            }

            // recursively walk reposts
            foreach (var repost in post.CopyHistory.OrderBy(x => x.Date))
            {
                yield return new Nested<Post>(repost, workDir, GetRepostDirName(repost));
            }
        }

        private string GetRepostDirName(Post repost)
        {
            var dateMaybe = repost.Date != null
                ? $"{repost.Date.Value:s}"
                : "no_date";
            return $"{dateMaybe} {repost.Id}";
        }

    }
}