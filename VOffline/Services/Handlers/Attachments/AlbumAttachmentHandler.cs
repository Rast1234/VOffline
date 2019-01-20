using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers.Attachments
{
    class AlbumAttachmentHandler : AttachmentHandlerBase<Album>
    {
        private readonly VkApiUtils vkApiUtils;

        public AlbumAttachmentHandler(VkApiUtils vkApiUtils)
        {
            this.vkApiUtils = vkApiUtils;
        }

        public override async Task<IEnumerable<object>> Process(OrderedAttachment<Album> attachment, CancellationToken token, ILog log)
        {
            // TODO: add workDir to model
            var albumWithPhoto = await vkApiUtils.ExpandAlbum(attachment.Data, token, log);
            return new List<object>(1) { albumWithPhoto};
        }
    }
}