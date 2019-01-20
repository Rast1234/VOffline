using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers.Attachments
{
    class AudioPlaylistAttachmentHandler : AttachmentHandlerBase<AudioPlaylist>
    {
        private readonly VkApiUtils vkApiUtils;

        public AudioPlaylistAttachmentHandler(VkApiUtils vkApiUtils)
        {
            this.vkApiUtils = vkApiUtils;
        }

        public override async Task<IEnumerable<object>> Process(OrderedAttachment<AudioPlaylist> attachment, CancellationToken token, ILog log)
        {
            // TODO: add workDir to model
            var playlistWithAudio = await vkApiUtils.ExpandPlaylist(attachment.Data, token, log);
            return new List<object>(1){playlistWithAudio};
        }
    }
}