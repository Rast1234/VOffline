using System.Linq;
using VkNet.Model.Attachments;

namespace VOffline.Services.Vk
{
    public static class AttachmentExtensions
    {
        public static string GetName(this Audio audio) => string.Join(" - ", new[] {audio.Artist, audio.Title}.Where(x => !string.IsNullOrEmpty(x)));

        public static string GetName(this Poll poll) => $"poll {poll.Id}";
    }
}