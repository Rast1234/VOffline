using log4net;
using VOffline.Models.Google;
using VOffline.Services.Token.Google;

namespace VOffline.Services.Token.Vk
{
    public class VkTokenReceiver
    {
        private FileCache<GoogleCredentials> CredentialsCache { get; }
        private VkHttpRequests VkHttpRequests { get; }
        private GoogleHttpRequests GoogleHttpRequests { get; }

        public VkTokenReceiver(FileCache<GoogleCredentials> credentialsCache, VkHttpRequests vkHttpRequests, GoogleHttpRequests googleHttpRequests)
        {
            CredentialsCache = credentialsCache;
            VkHttpRequests = vkHttpRequests;
            GoogleHttpRequests = googleHttpRequests;
        }

        public string GetToken(ILog log)
        {
            var receipt = GoogleHttpRequests.GetReceipt(log);
            var token = VkHttpRequests.GetNonRefreshedToken(log);
            var newToken = VkHttpRequests.RefreshToken(token, receipt, log);
            log.Info($"success! fresh token [{newToken}]");
            return newToken;
        }

        
    }

}
