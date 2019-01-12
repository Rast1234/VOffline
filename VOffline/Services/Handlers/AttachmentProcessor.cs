﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using VkNet;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers
{
    public class AttachmentProcessor
    {
        private readonly VkApi vkApi;
        private readonly FilesystemTools filesystemTools;
        private readonly DownloadQueueProvider downloadQueueProvider;

        public AttachmentProcessor(VkApi vkApi, FilesystemTools filesystemTools, DownloadQueueProvider downloadQueueProvider)
        {
            this.vkApi = vkApi;
            this.filesystemTools = filesystemTools;
            this.downloadQueueProvider = downloadQueueProvider;
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
                    await downloadQueueProvider.EnqueueAll(photo.ToDownloads(number, filesystemTools, workDir, log), token);
                    await photo.SaveText(number, filesystemTools, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.Audio audio:
                    await downloadQueueProvider.EnqueueAll(audio.ToDownloads(number, filesystemTools, workDir, log), token);
                    await audio.SaveLyrics(number, vkApi, filesystemTools, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.Document document:
                    await downloadQueueProvider.EnqueueAll(document.ToDownloads(number, filesystemTools, workDir, log), token);
                    break;
                case VkNet.Model.Attachments.Poll poll:
                    await downloadQueueProvider.EnqueueAll(poll.ToDownloads(number, filesystemTools, workDir, log), token);
                    await poll.SaveHumanReadableText(number, filesystemTools, workDir, token, log);
                    break;
                case VkNet.Model.Attachments.Link link:
                    await downloadQueueProvider.EnqueueAll(link.ToDownloads(number, filesystemTools, workDir, log), token);
                    break;

                case VkNet.Model.Attachments.AudioPlaylist audioPlaylist:
                case VkNet.Model.Attachments.Album album:
                case VkNet.Model.Attachments.ApplicationContent applicationContent:
                case VkNet.Model.Attachments.AudioMessage audioMessage:
                case VkNet.Model.Attachments.Gift gift:
                case VkNet.Model.Attachments.Graffiti graffiti:
                case VkNet.Model.Attachments.MarketAlbum marketAlbum:
                case VkNet.Model.Attachments.Note note:
                case VkNet.Model.Attachments.Page page:
                case VkNet.Model.Attachments.PrettyCards prettyCards:
                case VkNet.Model.Attachments.Sticker sticker:
                case VkNet.Model.Attachments.StringLink stringLink:
                case VkNet.Model.Attachments.Video video:
                case VkNet.Model.Attachments.WallReply wallReply:
                default:
                    log.Warn($"Not yet supported: attachment [{mediaAttachment.GetType().FullName}] {JsonConvert.SerializeObject(mediaAttachment)}");
                    break;
            }
        }

        public async Task ProcessAttachment(AudioCover audioCover, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            await downloadQueueProvider.EnqueueAll(audioCover.ToDownloads(filesystemTools, workDir, log), token);
        }
    }
}