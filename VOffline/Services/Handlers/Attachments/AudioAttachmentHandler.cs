using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Abstractions;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers.Attachments
{
    class AudioAttachmentHandler : AttachmentHandlerBase<Audio>
    {
        private readonly FileSystemTools fileSystemTools;
        private readonly IVkApi vkApi;

        public AudioAttachmentHandler(FileSystemTools fileSystemTools, IVkApi vkApi)
        {
            this.fileSystemTools = fileSystemTools;
            this.vkApi = vkApi;
        }

        public override async Task<IEnumerable<object>> Process(OrderedAttachment<Audio> attachment, CancellationToken token, ILog log)
        {
            await SaveLyrics(attachment, token, log);
            return ConvertToDownloads(attachment, log);
        }

        private async Task SaveLyrics(OrderedAttachment<Audio> attachment, CancellationToken token, ILog log)
        {
            if (attachment.Data.LyricsId != null)
            {
                var filename = $"{attachment.Number} {attachment.Data.GetName()}.txt";
                await fileSystemTools.WriteFileWithCompletionMark(attachment.WorkingDir, filename, async () =>
                {
                    var lyrics = await vkApi.Audio.GetLyricsAsync(attachment.Data.LyricsId.Value);
                    return lyrics.Text;
                }, token, log);
            }
        }

        private IEnumerable<Download> ConvertToDownloads(OrderedAttachment<Audio> attachment, ILog log)
        {
            var trackName = attachment.Data.GetName();
            if (attachment.Data.Url != null)
            {
                yield return new Download(attachment.Data.Url, attachment.WorkingDir, $"{attachment.Number} {trackName}.mp3");
            }
            else
            {
                fileSystemTools.CreateFile(attachment.WorkingDir, $"{attachment.Number} {trackName}.mp3.deleted", CreateMode.OverwriteExisting);
            }

        }
    }
}