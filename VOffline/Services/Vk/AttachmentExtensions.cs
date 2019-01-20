using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Services.Storage;

namespace VOffline.Services.Vk
{
    public static class AttachmentExtensions
    {
        public static string GetName(this Audio audio) => string.Join(" - ", new[] {audio.Artist, audio.Title}.Where(x => !string.IsNullOrEmpty(x)));

        public static string GetName(this Poll poll) => $"poll {poll.Id}";
    }
}