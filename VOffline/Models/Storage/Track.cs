using System;
using VkNet.Model;
using VkNet.Model.Attachments;

namespace VOffline.Models.Storage
{
    public class Track
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Artist { get; set; }
        public Uri Url { get; set; }
        public bool? Hq { get; set; }
        public string Lyrics { get; set; }

        public Track(Audio audio, Lyrics lyrics)
        {
            Id = audio.Id.Value;
            Name = audio.Title;
            Artist = audio.Artist;
            Url = audio.Url;
            Hq = audio.IsHq;
            Lyrics = lyrics?.Text;
        }
    }
}