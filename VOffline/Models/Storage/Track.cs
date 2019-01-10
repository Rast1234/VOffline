using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Services.Storage;

namespace VOffline.Models.Storage
{
    public class Track
    {
        public long Id { get; }
        public string Name { get; }
        public string Artist { get; }
        public Uri Url { get; }
        public bool? Hq { get; }
        public long? LyricsId { get; }

        public Track(Audio audio)
        {
            Id = audio.Id.Value;
            Name = audio.Title;
            Artist = audio.Artist;
            Url = audio.Url;
            Hq = audio.IsHq;
            LyricsId = audio.LyricsId;
        }

        public IEnumerable<IDownload> ToDownloads(FilesystemTools filesystemTools, DirectoryInfo dir)
        {
            var trackName = string.Join(" - ", new[] { Artist, Name }.Where(x => !string.IsNullOrEmpty(x)));
            if (Url != null)
            {
                yield return new Download(Url, dir, $"{trackName}.mp3");
            }
            else
            {
                filesystemTools.CreateUniqueFile(dir, $"{trackName}.mp3.deleted");
            }

            if (LyricsId != null)
            {
                yield return new LyricsVkDownload(LyricsId.Value, dir, $"{trackName}.txt");
            }
        }
    }
}