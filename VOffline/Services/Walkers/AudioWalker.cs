using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Walkers
{
    public class AudioWalker : WalkerBase<AudioCategory>
    {
        private readonly VkApiUtils vkApiUtils;
        private readonly IWalker<PlaylistWithAudio> playlistWalker;

        public AudioWalker(VkApiUtils vkApiUtils, FilesystemTools filesystemTools, IWalker<PlaylistWithAudio> playlistWalker) : base(filesystemTools)
        {
            this.vkApiUtils = vkApiUtils;
            this.playlistWalker = playlistWalker;
        }

        public override async Task ProcessInternal(AudioCategory audio, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            var vkPlaylists = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Playlists(audio.OwnerId), 200, token, log);
            log.Debug($"Audio: {vkPlaylists.Count} playlists");
            var expandTasks = vkPlaylists.Select(p => vkApiUtils.ExpandPlaylist(p, token, log));
            var playlists = await Task.WhenAll(expandTasks);
            log.Debug($"Audio: {playlists.Sum(p => p.Audio.Count)} in {playlists.Length} playlists");

            var allAudios = await vkApiUtils.GetAllPagesAsync(vkApiUtils.Audios(audio.OwnerId), long.MaxValue, token, log);
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

            var allTasks = allPlaylists
                .OrderBy(p => p.Playlist.CreateTime)
                .Select(p => playlistWalker.Process(p, workDir, token, log));
            await Task.WhenAll(allTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(AudioCategory audio, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, "Audio", CreateMode.OverwriteExisting);
    }
}
