﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using VkNet;
using VkNet.Enums.SafetyEnums;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Vk
{
    public static class AttachmentExtensions
    {
        public static IEnumerable<IDownload> ToDownloads(this Audio audio, int i, FilesystemTools filesystemTools, DirectoryInfo dir, ILog log)
        {
            var trackName = audio.GetName();
            if (audio.Url != null)
            {
                yield return new Download(audio.Url, dir, $"{i} {trackName}.mp3");
            }
            else
            {
                filesystemTools.CreateFile(dir, $"{i} {trackName}.mp3.deleted", CreateMode.OverwriteExisting);
            }
        }

        public static IEnumerable<IDownload> ToDownloads(this Photo photo, int i, FilesystemTools filesystemTools, DirectoryInfo dir, ILog log)
        {
            // TODO: not sure about ANYTHING here
            var url = photo.BigPhotoSrc
                      ?? photo.Photo2560
                      ?? photo.PhotoSrc
                      ?? photo.Photo1280
                      ?? photo.Photo807
                      ?? photo.Photo604
                      ?? photo.SmallPhotoSrc
                      ?? photo.Photo200
                      ?? photo.Photo130
                      ?? photo.Photo100
                      ?? photo.Photo75
                      ?? photo.Photo50
                ?? photo.Sizes
                          .Select(s => (square:s.Width*s.Height, size:s))
                          .OrderByDescending(x => x.square)
                          .FirstOrDefault(s => s.square > 0)   // width/height can be null
                          .size?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.W)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.Z)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.Y)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.X)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.R)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.Q)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.P)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.O)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.M)?.Url
                ?? photo.Sizes.FirstOrDefault(s => s.Type == PhotoSizeType.S)?.Url
                ;

            // TODO: i guess it's always jpeg?
            var ext = Path.HasExtension(url?.AbsoluteUri) ? Path.GetExtension(url?.AbsoluteUri) : ".jpg";
            if (url != null)
            {
                var file = $"{i}_{photo.Id}{ext}";
                yield return new Download(url, dir, file);
            }
            else
            {
                log.Warn($"Photo with no url! {JsonConvert.SerializeObject(photo)}");
            }
        }

        public static IEnumerable<IDownload> ToDownloads(this Video video, int i, FilesystemTools filesystemTools, DirectoryInfo dir, ILog log)
        {
            var vkUrl = video.Files?.Mp4_1080
                        ?? video.Files?.Mp4_720
                        ?? video.Files?.Mp4_480
                        ?? video.Files?.Mp4_360
                        ?? video.Files?.Mp4_240;
            if (vkUrl != null)
            {
                yield return new Download(vkUrl, dir, video.Title);
            }
            else if(video.Files?.External != null)
            {
                log.Warn($"Video {video.Id} [{video.Title}] is external");
                yield return new Download(video.Files.External, dir, video.Title);
            }
            else
            {
                log.Warn($"Video {video.Id} [{video.Title}] has no links. Trying to compose vk url");
                var file = filesystemTools.CreateFile(dir, $"{i} {video.Id}.txt", CreateMode.OverwriteExisting);
                File.WriteAllText(file.FullName, $"https://vk.com/video{video.OwnerId}_{video.Id}");
            }
        }

        public static IEnumerable<IDownload> ToDownloads(this Document document, int i, FilesystemTools filesystemTools, DirectoryInfo dir, ILog log)
        {
            // TODO: looks like Title already has Extension
            yield return new Download(new Uri(document.Uri), dir, $"{i} {document.Title}");
        }

        public static IEnumerable<IDownload> ToDownloads(this Poll poll, int i, FilesystemTools filesystemTools, DirectoryInfo dir, ILog log)
        {
            var photos = new List<Photo>();
            if (poll.Photo != null)
            {
                photos.Add(poll.Photo);
            }
            if (poll?.Background?.Images != null)
            {
                photos.AddRange(poll.Background.Images);
            }

            if (!photos.Any())
            {
                yield break;
            }
            var pollDir = filesystemTools.CreateSubdir(dir, $"{i} {poll.GetName()}", CreateMode.OverwriteExisting);
            var imageDownloads = photos.SelectMany((p, j) => p.ToDownloads(j, filesystemTools, pollDir, log));
            foreach (var imageDownload in imageDownloads)
            {
                yield return imageDownload;
            }
        }

        public static IEnumerable<IDownload> ToDownloads(this Link link, int i, FilesystemTools filesystemTools, DirectoryInfo dir, ILog log)
        {
            yield return new Download(link.Uri, dir, link.Title);
        }

        public static IEnumerable<IDownload> ToDownloads(this AudioCover cover, FilesystemTools filesystemTools, DirectoryInfo dir, ILog log)
        {
            var best = cover.Photo600
                        ?? cover.Photo300
                        ?? cover.Photo270
                        ?? cover.Photo135
                        ?? cover.Photo68
                        ?? cover.Photo34;
            var image = best == null
                ? null
                : new Uri(best);

            // TODO: i guess it's always jpeg?
            var ext = Path.HasExtension(image?.AbsoluteUri) ? Path.GetExtension(image?.AbsoluteUri) : ".jpg";
            if (image == null)
            {
                yield break;
            }
            var file = $"__cover{ext}";
            yield return new Download(image, dir, file);
        }

        public static string Serialize(this Poll poll)
        {
            var sb = new StringBuilder();
            sb.AppendLine(poll.Question);

            sb.Append($"{poll.Created.Value:O}");
            if (poll.EndDate.HasValue)
            {
                sb.Append($" - {poll.Closed.Value:O}");
            }
            sb.AppendLine();
            
            sb.AppendLine($"votes: {poll.Votes}");
            sb.AppendLine($"flags: alonymous={poll.Anonymous},");
            foreach (var answer in poll.Answers.OrderByDescending(a => a.Rate))
            {
                var voteState = poll.AnswerId == answer.Id.Value
                                || poll.AnswerIds.Contains(answer.Id.Value)
                    ? "[v]"
                    : "[ ]";
                sb.AppendLine($"{voteState} {answer.Text} ({answer.Rate:F}%, {answer.Votes})");
            }

            return sb.ToString();
        }

        public static string Serialize(this Link link)
        {
            var sb = new StringBuilder();
            sb.AppendLine(link.Title);
            sb.AppendLine(link.Caption);
            sb.AppendLine(link.Description);
            sb.AppendLine($"{link.Uri}");
            return sb.ToString();
        }

        public static string Serialize(this Comment comment)
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

        public static string GetName(this Audio audio) => string.Join(" - ", new[] { audio.Artist, audio.Title }.Where(x => !string.IsNullOrEmpty(x)));

        public static string GetName(this Poll poll) => $"poll {poll.Id}";

        public static async Task SaveText(this Photo photo, int i, FilesystemTools filesystemTools, DirectoryInfo dir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(photo.Text))
            {
                var textFile = filesystemTools.CreateFile(dir, $"{i} {photo.Id}.txt", CreateMode.OverwriteExisting);
                await File.WriteAllTextAsync(textFile.FullName, photo.Text, token);
            }
        }

        public static async Task SaveLyrics(this Audio audio, int i, VkApi vkApi, FilesystemTools filesystemTools, DirectoryInfo dir, CancellationToken token, ILog log)
        {
            if (audio.LyricsId != null)
            {
                var filename = $"{i} {audio.GetName()}.txt";
                await filesystemTools.WriteFileWithCompletionMark(dir, filename, async () =>
                {
                    var lyrics = await vkApi.Audio.GetLyricsAsync(audio.LyricsId.Value);
                    return lyrics.Text;
                }, token, log);
            }
        }

        public static async Task SaveHumanReadableText(this Poll poll, int i, FilesystemTools filesystemTools, DirectoryInfo dir, CancellationToken token, ILog log)
        {
            var textFile = filesystemTools.CreateFile(dir, $"{i} {poll.GetName()}.txt", CreateMode.OverwriteExisting);
            await File.WriteAllTextAsync(textFile.FullName, poll.Serialize(), token);
        }

        public static async Task SaveHumanReadableText(this Link link, int i, FilesystemTools filesystemTools, DirectoryInfo dir, CancellationToken token, ILog log)
        {
            var textFile = filesystemTools.CreateFile(dir, $"{i} {link.Title}.txt", CreateMode.OverwriteExisting);
            await File.WriteAllTextAsync(textFile.FullName, link.Serialize(), token);
        }

        public static async Task SaveHumanReadableText(this IReadOnlyList<Comment> comments, FilesystemTools filesystemTools, DirectoryInfo dir, CancellationToken token, ILog log)
        {
            var data = string.Join("\n\n", comments.Select(c => c.Serialize()));
            var textFile = filesystemTools.CreateFile(dir, $"comments.txt", CreateMode.OverwriteExisting);
            await File.WriteAllTextAsync(textFile.FullName, data, token);
        }
    }
}