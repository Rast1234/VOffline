using log4net;
using VOffline.Models;
using VOffline.Models.Google;
using VOffline.Models.Vk;
using VOffline.Services.Google;

namespace VOffline.Services.Vk
{
    public class VkTokenReceiver
    {
        private FileCache<VkCredentials> VkCredentialsCache { get; }
        private FileCache<GoogleCredentials> CredentialsCache { get; }
        private VkHttpRequests VkHttpRequests { get; }
        private GoogleHttpRequests GoogleHttpRequests { get; }

        public VkTokenReceiver(FileCache<VkCredentials> vkCredentialsCache, FileCache<GoogleCredentials> credentialsCache, VkHttpRequests vkHttpRequests, GoogleHttpRequests googleHttpRequests)
        {
            VkCredentialsCache = vkCredentialsCache;
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
