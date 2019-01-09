using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using VkNet;
using VkNet.Enums;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;
using VOffline.Models.Storage;

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
                return -1 * long.Parse(communityMatch.Groups[2].Value);
            }

            // any user eg. id123
            var personalMatch = PersonalPattern.Match(target);
            if (personalMatch.Success)
            {
                return long.Parse(personalMatch.Groups[2].Value);
            }

            // any id eg. 123 or -123
            var digitalMatch = DigitalPattern.Match(target);
            if (digitalMatch.Success)
            {
                return long.Parse(digitalMatch.Groups[2].Value);
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
                return string.Join(" ", user.LastName, user.FirstName, GroupOrEmpty(" - ", user.Id.ToString(), user.ScreenName, user.Domain));
            }
            var groups = await vkApi.Groups.GetByIdAsync(null, (-1*id).ToString(), GroupsFields.All);
            var group = groups[0];
            return string.Join(" ", group.Name, GroupOrEmpty(" - ", group.Id.ToString(), group.ScreenName, group.Type.ToString()));
        }
        
        private static string GroupOrEmpty(string separator, params string[] parts)
        {
            var all = string.Join(separator, parts);
            return string.IsNullOrEmpty(all)
                ? string.Empty
                : $"({all})";
        }

        private static readonly Regex CommunityPattern = new Regex(@"^(public|club|event)(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PersonalPattern = new Regex(@"^(id)(\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex DigitalPattern = new Regex(@"^(-?\d+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        
    }

    public class UserAgentProvider
    {
        public string UserAgent => UserAgentValue;
        private const string UserAgentValue = "KateMobileAndroid/51.2 lite-443 (Android 4.4.2; SDK 19; x86; unknown Android SDK built for x86; en)";
    }
}