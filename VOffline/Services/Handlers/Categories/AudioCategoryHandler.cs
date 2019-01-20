using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers.Categories
{
    public class AudioCategoryHandler : CategoryHandlerBase<AudioCategory>
    {
        private readonly VkApiUtils vkApiUtils;

        public AudioCategoryHandler(VkApiUtils vkApiUtils, FileSystemTools fileSystemTools) : base(fileSystemTools)
        {
            this.vkApiUtils = vkApiUtils;
        }

        public override async Task<IEnumerable<object>> ProcessInternal(AudioCategory audio, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var vkPlaylists = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Playlists(audio.OwnerId), 200, token, log);
            log.Debug($"Audio: {vkPlaylists.Count} playlists");
            var expandTasks = vkPlaylists.Select(p => vkApiUtils.ExpandPlaylist(p, token, log));
            var playlists = await Task.WhenAll(expandTasks);
            log.Debug($"Audio: {playlists.Sum(p => p.Audio.Count)} in {playlists.Length} playlists");

            var allAudios = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Audios(audio.OwnerId), 100, token, log);
            var audioInPlaylists = playlists
                .SelectMany(p => p.Audio.Select(t => t.Id))
                .ToHashSet();
            var uniqueAudios = allAudios
                .Where(a => !audioInPlaylists.Contains(a.Id.Value))
                .ToList();
            var defaultPlaylist = new PlaylistWithAudio(uniqueAudios);
            log.Debug($"Audio: {allAudios.Count} total, {defaultPlaylist.Audio.Count} unique in default playlist");

            var allPlaylists = playlists.ToList();
            allPlaylists.Add(defaultPlaylist);
            return allPlaylists
                .OrderBy(p => p.Playlist.CreateTime)
                .Select(p => new Nested<PlaylistWithAudio>(p, workDir, $"{p.Playlist.Title}"));
        }

        
    }
}
