using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;

namespace VOffline.Services.Vk
{
    public class VkApiUtils
    {
        private readonly VkApi vkApi;

        public VkApiUtils(VkApi vkApi)
        {
            this.vkApi = vkApi;
        }

        public async Task<long> ResolveId(string target)
        {
            // any community type with id, eg. club123 or event123
            var communityMatch = CommunityPattern.Match(target);
            if (communityMatch.Success)
            {
                return -1 * Int64.Parse(communityMatch.Groups[2].Value);
            }

            // any user eg. id123
            var personalMatch = PersonalPattern.Match(target);
            if (personalMatch.Success)
            {
                return Int64.Parse(personalMatch.Groups[2].Value);
            }

            // any id eg. 123 or -123
            var digitalMatch = DigitalPattern.Match(target);
            if (digitalMatch.Success)
            {
                return Int64.Parse(digitalMatch.Groups[1].Value);
            }

            // any screen name
            var vkObj = await vkApi.Utils.ResolveScreenNameAsync(target);
            switch (vkObj.Type)
            {
                case VkObjectType.User:
                    return vkObj.Id.Value;
                case VkObjectType.Group:
                    return -1 * vkObj.Id.Value;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public async Task<string> GetName(long id)
        {
            if (id >= 0)
            {
                var users = await vkApi.Users.GetAsync(new []{id}, ProfileFields.All);
                var user = users[0];
                return String.Join(" ", user.LastName, user.FirstName, GroupOrEmpty(" - ", user.Id.ToString(), user.ScreenName, user.Domain));
            }
            var groups = await vkApi.Groups.GetByIdAsync(null, (-1*id).ToString(), GroupsFields.All);
            var group = groups[0];
            return String.Join(" ", group.Name, GroupOrEmpty(" - ", group.Id.ToString(), group.ScreenName, group.Type.ToString()));
        }
        
        private static string GroupOrEmpty(string separator, params string[] parts)
        {
            var all = String.Join(separator, parts);
            return String.IsNullOrEmpty(all)
                ? String.Empty
                : $"({all})";
        }

        private static readonly Regex CommunityPattern = new Regex(@"^(public|club|event)(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PersonalPattern = new Regex(@"^(id)(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DigitalPattern = new Regex(@"^(-?\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void ThrowIfCountMismatch(decimal expectedTotal, decimal resultCount)
        {
            if (resultCount != expectedTotal)
            {
                throw new InvalidOperationException($"Expected {expectedTotal} items, got {resultCount}. Maybe they were created/deleted, try again.");
            }
        }
    }
}