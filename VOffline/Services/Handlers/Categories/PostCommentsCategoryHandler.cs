using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers.Categories
{
    public class PostCommentsCategoryHandler : CategoryHandlerBase<PostComments>
    {
        private readonly VkApiUtils vkApiUtils;

        public PostCommentsCategoryHandler(VkApiUtils vkApiUtils, FileSystemTools fileSystemTools) : base(fileSystemTools)
        {
            this.vkApiUtils = vkApiUtils;
        }

        public override async Task<IEnumerable<object>> ProcessInternal(PostComments postComments, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var allComments = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Comments(postComments.Post), 100, token, log);
            log.Debug($"Post {postComments.Post.Id} has {allComments.Count} comments");
            var byDate = allComments
                .OrderBy(c => c.Date)
                .ToList();
            await SaveHumanReadableText(byDate, workDir, token, log);
            return GetComments(byDate, workDir);
        }

        private IEnumerable<object> GetComments(IReadOnlyList<Comment> byDate, DirectoryInfo workDir)
        {
            foreach (var comment in byDate.Where(c => c.Attachments.Count > 0))
            {
                yield return new Nested<Comment>(comment, workDir, $"{comment.Date.Value:s} {comment.Id}");
            }
        }

        private async Task SaveHumanReadableText(IReadOnlyList<Comment> comments, DirectoryInfo dir, CancellationToken token, ILog log)
        {
            var data = string.Join("\n\n", comments.Select(Serialize));
            var textFile = fileSystemTools.CreateFile(dir, $"comments.txt", CreateMode.OverwriteExisting);
            await File.WriteAllTextAsync(textFile.FullName, data, token);
        }

        private static string Serialize(Comment comment)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{comment.Date} {comment.FromId}");
            sb.AppendLine(comment.Text);
            sb.Append($"likes={comment.Likes?.Count}, attachments={comment.Attachments?.Count}, id={comment.Id}");
            if (comment.ReplyToCommentId != null || comment.ReplyToUserId != null)
            {
                sb.Append($", reply to user {comment.ReplyToUserId} comment {comment.ReplyToCommentId}");
            }

            return sb.ToString();
        }


    }
}