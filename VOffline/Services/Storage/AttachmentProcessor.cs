using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Walkers;
using VOffline.Services.Vk;

namespace VOffline.Services.Storage
{
    public class AttachmentProcessor
    {
        public IServiceProvider ServiceProvider { get; }
        private readonly VkApi vkApi;
        private readonly VkApiUtils vkApiUtils;
        private readonly FilesystemTools filesystemTools;
        private readonly QueueProvider queueProvider;

        public AttachmentProcessor(VkApi vkApi, VkApiUtils vkApiUtils, FilesystemTools filesystemTools, QueueProvider queueProvider, IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            this.vkApi = vkApi;
            this.vkApiUtils = vkApiUtils;
            this.filesystemTools = filesystemTools;
            this.queueProvider = queueProvider;
        }

        public async Task ProcessAttachment(Attachment attachment, int number, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            await ProcessAttachment(attachment.Instance, number, workDir, token, log);
        }

        public async Task ProcessAttachment(MediaAttachment mediaAttachment, int number, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            switch (mediaAttachment)
            {
                case VkNet.Model.Attachments.Photo photo:
                    await queueProvider.EnqueueAllDownloads(photo.ToDownloads(number, filesystemTools, workDir, log), token);
                    await photo.SaveText(number, filesystemTools, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.Audio audio:
                    await queueProvider.EnqueueAllDownloads(audio.ToDownloads(number, filesystemTools, workDir, log), token);
                    await audio.SaveLyrics(number, vkApi, filesystemTools, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.Document document:
                    await queueProvider.EnqueueAllDownloads(document.ToDownloads(number, filesystemTools, workDir, log), token);
                    break;
                case VkNet.Model.Attachments.Poll poll:
                    await queueProvider.EnqueueAllDownloads(poll.ToDownloads(number, filesystemTools, workDir, log), token);
                    await poll.SaveHumanReadableText(number, filesystemTools, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.Link link:
                    await queueProvider.EnqueueAllDownloads(link.ToDownloads(number, filesystemTools, workDir, log), token);
                    await link.SaveHumanReadableText(number, filesystemTools, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.AudioPlaylist audioPlaylist:
                    var playlistWithAudio = await vkApiUtils.ExpandPlaylist(audioPlaylist, token, log);
                    var plalistWalker = ServiceProvider.GetRequiredService<IWalker<PlaylistWithAudio>>();
                    await plalistWalker.Process(playlistWithAudio, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.Album album:
                    var albumWithPhoto = await vkApiUtils.ExpandAlbum(album, token, log);
                    var albumWalker = ServiceProvider.GetRequiredService<IWalker<AlbumWithPhoto>>();
                    await albumWalker.Process(albumWithPhoto, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.Video video:
                    await queueProvider.EnqueueAllDownloads(video.ToDownloads(number, filesystemTools, workDir, log), token);
                    break;

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
        }

        public async Task ProcessAttachment(AudioCover audioCover, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            await queueProvider.EnqueueAllDownloads(audioCover.ToDownloads(filesystemTools, workDir, log), token);
        }
    }
}