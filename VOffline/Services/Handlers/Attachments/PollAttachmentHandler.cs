using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model.Attachments;
using VOffline.Models.Storage;
using VOffline.Services.Storage;
using VOffline.Services.Vk;

namespace VOffline.Services.Handlers.Attachments
{
    class PollAttachmentHandler : AttachmentHandlerBase<Poll>
    {
        private readonly FileSystemTools fileSystemTools;

        public PollAttachmentHandler(FileSystemTools fileSystemTools)
        {
            this.fileSystemTools = fileSystemTools;
        }

        public override async Task<IEnumerable<object>> Process(OrderedAttachment<Poll> attachment, CancellationToken token, ILog log)
        {
            var poll = attachment.Data;
            await SaveHumanReadableText(attachment, token, log);

            var photos = new List<Photo>();
            if (poll.Photo != null)
            {
                photos.Add(poll.Photo);
            }

            if (poll.Background?.Images != null)
            {
                photos.AddRange(poll.Background.Images);
            }

            if (!photos.Any())
            {
                return Nothing;
            }

            var pollDir = fileSystemTools.CreateSubdir(attachment.WorkingDir, $"{attachment.Number} {poll.GetName()}", CreateMode.OverwriteExisting);
            var result = photos.Select((p, i) => new OrderedAttachment<Photo>(p, i, pollDir));
            return result;
        }

        private async Task SaveHumanReadableText(OrderedAttachment<Poll> attachment, CancellationToken token, ILog log)
        {
            var textFile = fileSystemTools.CreateFile(attachment.WorkingDir, $"{attachment.Number} {attachment.Data.GetName()}.txt", CreateMode.OverwriteExisting);
            await File.WriteAllTextAsync(textFile.FullName, Serialize(attachment.Data), token);
        }

        private static string Serialize(Poll poll)
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
            sb.AppendLine($"flags: anonymous={poll.Anonymous},");
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
    }
}