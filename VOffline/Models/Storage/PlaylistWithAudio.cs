using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Model.Attachments;

namespace VOffline.Models.Storage
{
    public class PlaylistWithAudio
    {
        public AudioPlaylist Playlist { get; }
        public IReadOnlyList<Audio> Audio { get; }

        public PlaylistWithAudio(AudioPlaylist playlist, IReadOnlyList<Audio> audio)
        {
            Playlist = playlist;
            Audio = audio
                .OrderBy(a => a.Date)
                .ToList();
        }

        public PlaylistWithAudio(IReadOnlyList<Audio> audio)
        {

            this.Playlist = new AudioPlaylist
            {
                Id = -1,
                Title = "__default",
                Description = "Tracks without playlist",
                CreateTime = DateTime.MinValue
            };
            this.Audio = audio
                .OrderBy(a => a.Date)
                .ToList();
        }
    }
}
