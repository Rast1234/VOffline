using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VOffline.Models.Storage;

namespace VOffline.Services.Handlers.Attachments
{
    public abstract class AttachmentHandlerBase<TAttachment> : IHandler<OrderedAttachment<TAttachment>>
    {
        public abstract Task<IEnumerable<object>> Process(OrderedAttachment<TAttachment> attachment, CancellationToken token, ILog log);

        protected static readonly IEnumerable<object> Nothing = Enumerable.Empty<object>();

        /*
        switch (mediaAttachment)
            {
                case VkNet.Model.Attachments.Note note:  // note и page похожи
                case VkNet.Model.Attachments.Page page:

                

                // остальное похоже на хлам
                case VkNet.Model.Attachments.ApplicationContent applicationContent:
                case VkNet.Model.Attachments.AudioMessage audioMessage:
                case VkNet.Model.Attachments.Gift gift:
                case VkNet.Model.Attachments.Graffiti graffiti:
                case VkNet.Model.Attachments.MarketAlbum marketAlbum:
                case VkNet.Model.Attachments.PrettyCards prettyCards:
                case VkNet.Model.Attachments.Sticker sticker:
                case VkNet.Model.Attachments.StringLink stringLink:
                case VkNet.Model.Attachments.WallReply wallReply:
                default:
                    log.Warn($"Not yet supported: attachment [{mediaAttachment.GetType().FullName}] {JsonConvert.SerializeObject(mediaAttachment)}");
                    break;
            }
         */
    }
}