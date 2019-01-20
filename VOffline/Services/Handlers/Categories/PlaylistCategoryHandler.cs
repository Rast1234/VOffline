using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers.Categories
{
    public class PlaylistCategoryHandler : CategoryHandlerBase<PlaylistWithAudio>
    {

        public PlaylistCategoryHandler(FileSystemTools fileSystemTools) : base(fileSystemTools)
        {
        }

        public override async Task<IEnumerable<object>> ProcessInternal(PlaylistWithAudio playlistWithAudio, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(playlistWithAudio.Playlist.Description))
            {
                var text = fileSystemTools.CreateFile(workDir, $"__description.txt", CreateMode.OverwriteExisting);
                await File.WriteAllTextAsync(text.FullName, playlistWithAudio.Playlist.Description, token);
            }

            return GetAttachments(playlistWithAudio, workDir);
        }

        private IEnumerable<object> GetAttachments(PlaylistWithAudio playlistWithAudio, DirectoryInfo workDir)
        {
            if (playlistWithAudio.Playlist.Cover != null)
            {
                yield return new OrderedAttachment<AudioCover>(playlistWithAudio.Playlist.Cover, -1, workDir);
            }

            var i = 1;
            foreach (var audio in playlistWithAudio.Audio)
            {
                yield return new OrderedAttachment<Audio>(audio, i, workDir);
                i++;
            }
        }
    }
}