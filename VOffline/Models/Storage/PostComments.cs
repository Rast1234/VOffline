using System;
using VkNet.Model.Attachments;

namespace VOffline.Models.Storage
{
    public class PostComments
    {
        public PostComments(Post post)
        {
            Post = post ?? throw new ArgumentNullException(nameof(post));
        }

        public Post Post { get; }
    }
}