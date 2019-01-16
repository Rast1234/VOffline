using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers
{
    public class PlaylistHandler : HandlerBase<PlaylistWithAudio>
    {
        private readonly AttachmentProcessor attachmentProcessor;

        public PlaylistHandler(FilesystemTools filesystemTools, AttachmentProcessor attachmentProcessor) : base(filesystemTools)
        {
            this.attachmentProcessor = attachmentProcessor;
        }

        public override async Task ProcessInternal(PlaylistWithAudio playlistWithAudio, DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(playlistWithAudio.Playlist.Description))
            {
                var text = filesystemTools.CreateFile(workDir, $"__description.txt", CreateMode.OverwriteExisting);
                await File.WriteAllTextAsync(text.FullName, playlistWithAudio.Playlist.Description, token);
            }

            if (playlistWithAudio.Playlist.Cover != null)
            {
                await attachmentProcessor.ProcessAttachment(playlistWithAudio.Playlist.Cover, workDir, token, log);
            }
            
            var attachmentTasks = playlistWithAudio.Audio.Select((a, i) => attachmentProcessor.ProcessAttachment(a, i+1, workDir, token, log));
            await Task.WhenAll(attachmentTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(PlaylistWithAudio playlistWithAudio, DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, $"{playlistWithAudio.Playlist.Title}", CreateMode.OverwriteExisting);
    }
}