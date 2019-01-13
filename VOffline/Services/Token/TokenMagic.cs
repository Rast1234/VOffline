using log4net;
using VOffline.Models.Vk;
using VOffline.Services.Token.Google;
using VOffline.Services.Token.Vk;

namespace VOffline.Services.Token
{
    public class TokenMagic
    {
        private AndroidAuth AndroidAuth { get; }
        private VkTokenReceiver VkTokenReceiver { get; }
        public FileCache<VkToken> VkToken { get; }

        public TokenMagic(AndroidAuth androidAuth, VkTokenReceiver vkTokenReceiver, FileCache<VkToken> vkToken)
        {
            AndroidAuth = androidAuth;
            VkTokenReceiver = vkTokenReceiver;
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