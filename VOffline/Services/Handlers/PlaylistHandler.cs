using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;

namespace VOffline.Services.Handlers
{
    public class PlaylistHandler : HandlerBase
    {
        private readonly PlaylistWithAudio playlistWithAudio;
        private readonly AttachmentProcessor attachmentProcessor;

        public PlaylistHandler(PlaylistWithAudio playlistWithAudio, FilesystemTools filesystemTools, AttachmentProcessor attachmentProcessor) : base(filesystemTools)
        {
            this.playlistWithAudio = playlistWithAudio;
            this.attachmentProcessor = attachmentProcessor;
        }

        public override async Task ProcessInternal(DirectoryInfo workDir, CancellationToken token, ILog log)
        {
            if (!string.IsNullOrWhiteSpace(playlistWithAudio.Playlist.Description))
            {
                var text = filesystemTools.CreateFile(workDir, $"__description.txt", CreateMode.MergeWithExisting);
                File.WriteAllText(text.FullName, playlistWithAudio.Playlist.Description);
            }

            if (playlistWithAudio.Playlist.Cover != null)
            {
                await attachmentProcessor.ProcessAttachment(playlistWithAudio.Playlist.Cover, workDir, token, log);
            }
            
            var attachmentTasks = playlistWithAudio.Audio.Select((a, i) => attachmentProcessor.ProcessAttachment(a, i, workDir, token, log));
            await Task.WhenAll(attachmentTasks);
        }

        public override DirectoryInfo GetWorkingDirectory(DirectoryInfo parentDir) => filesystemTools.CreateSubdir(parentDir, $"{playlistWithAudio.Playlist.Title}", CreateMode.MergeWithExisting);
    }
}