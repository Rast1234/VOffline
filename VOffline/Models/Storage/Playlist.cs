using System;
using System.Collections.Generic;
using VkNet.Model;

namespace VOffline.Models.Storage
{
    public class Playlist
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Uri Cover { get; set; }

        public Playlist(AudioPlaylist playlist, IReadOnlyList<Track> tracks)
        {
            Id = playlist.Id;
            Name = playlist.Title;
            Description = playlist.Description;
            Cover = GetBestImage(playlist.Cover);
            Tracks = tracks;
        }

        public Playlist(IReadOnlyList<Track> tracks)
        {
            Id = -1;
            Name = "__default";
            Description = "Tracks without playlist";
            Cover = null;
            Tracks = tracks;
        }

        public IReadOnlyList<Track> Tracks { get; set; }

        private static Uri GetBestImage(AudioCover cover)
        {
            // TODO: theoretically images could be different or missing, but looks like this works fine
            return string.IsNullOrEmpty(cover?.Photo600)
                ? null
                : new Uri(cover?.Photo600);
        }
    }
}
