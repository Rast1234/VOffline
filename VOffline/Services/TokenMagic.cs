using log4net;
using VOffline.Models;
using VOffline.Models.Vk;
using VOffline.Services.Google;
using VOffline.Services.Vk;

namespace VOffline.Services
{
    public class TokenMagic
    {
        private AndroidAuth AndroidAuth { get; }
        private VkTokenReceiver VkTokenReceiver { get; }
        private FileCache<VkCredentials> VkCredentials { get; }
        public FileCache<VkToken> VkToken { get; }

        public TokenMagic(AndroidAuth androidAuth, VkTokenReceiver vkTokenReceiver, FileCache<VkCredentials> vkCredentials, FileCache<VkToken> vkToken)
        {
            AndroidAuth = androidAuth;
            VkTokenReceiver = vkTokenReceiver;
            VkCredentials = vkCredentials;
            VkToken = vkToken;
        }

        public VkToken GetTokenFromScratch(ILog log)
        {
            if (VkToken.Value != null)
            {
                return VkToken.Value;
            }

            AndroidAuth.GetCredentials(log);
            var token = VkTokenReceiver.GetToken(log);
            VkToken.Value = new VkToken
            {
                Token = token
            };
            return VkToken.Value;
        }
    }
}