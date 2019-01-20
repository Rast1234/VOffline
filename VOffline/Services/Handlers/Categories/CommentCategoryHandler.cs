using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers.Categories
{
    public class CommentCategoryHandler : CategoryHandlerBase<Comment>
    {

        public CommentCategoryHandler(FileSystemTools fileSystemTools) : base(fileSystemTools)
        {
        }

        public override async Task<IEnumerable<object>> ProcessInternal(Comment comment, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            return GetAttachments(comment, workDir);
        }

        private IEnumerable<object> GetAttachments(Comment comment, DirectoryInfo workDir)
        {
            var i = 1;
            foreach (var attachment in comment.Attachments)
            {
                // holy moly, something went wrong
                var ordAttGeneric = typeof(OrderedAttachment<>);
                Type[] typeArgs = { attachment.Type };
                var ordAttType = ordAttGeneric.MakeGenericType(typeArgs);
                var result = Activator.CreateInstance(ordAttType, attachment.Instance, i, workDir);
                yield return result;
                i++;
            }
        }
    }
}