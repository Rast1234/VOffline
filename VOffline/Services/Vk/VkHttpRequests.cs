using System;
using log4net;
using Newtonsoft.Json;
using RestSharp;
using VOffline.Models;
using VOffline.Models.Vk;

namespace VOffline.Services.Vk
{
    public class VkHttpRequests
    {
        private FileCache<VkCredentials> VkCredentialsCache { get; }

        public VkHttpRequests(FileCache<VkCredentials> vkCredentialsCache)
        {
            VkCredentialsCache = vkCredentialsCache;
        }

        public string GetNonRefreshedToken(ILog log)
        {
            var client = GetClient("https://oauth.vk.com/token");
            var request = new RestRequest(Method.GET);
            request.AddQueryParameter("grant_type", "password");
            request.AddQueryParameter("client_id", "2685278");
            request.AddQueryParameter("client_secret", "lxhD8OD7dMsqtXIm5IUY");
            request.AddQueryParameter("username", VkCredentialsCache.Value.Login);
            request.AddQueryParameter("password", VkCredentialsCache.Value.Password);
            request.AddQueryParameter("v", VkApiVersion);
            request.AddQueryParameter("scope", "audio,offline");
            log.Debug($"request non-refreshed token");
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException($"{nameof(GetNonRefreshedToken)} bad response: {response.StatusCode}\n{response.Content}");
            }

            var json = JsonConvert.DeserializeObject<dynamic>(response.Content);
            if (json.user_id == null)
            {
                throw new InvalidOperationException($"{nameof(GetNonRefreshedToken)} bad response: user_id is null\n{response.Content}");
            }

            return (string) json.access_token;
        }

        public string RefreshToken(string token, string receipt, ILog log)
        {
            var client = GetClient("https://api.vk.com/method/auth.refreshToken");
            var request = new RestRequest(Method.GET);
            request.AddQueryParameter("access_token", token);
            request.AddQueryParameter("receipt", receipt);
            request.AddQueryParameter("v", VkApiVersion);
            log.Debug($"refresh token");
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException($"{nameof(RefreshToken)} bad response: {response.StatusCode}\n{response.Content}");
            }

            var json = JsonConvert.DeserializeObject<dynamic>(response.Content);
            var newToken = (string)json.response.token;
            if (newToken == token)
            {
                throw new InvalidOperationException($"{nameof(RefreshToken)} bad response: token not refreshed\n{response.Content}");
            }

            return newToken;
        }

        private RestClient GetClient(string url)
        {
            var client = new RestClient(url)
            {
                UserAgent = VkUserAgent
            };
            client.RemoteCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            return client;
        }

        private const string VkUserAgent = "KateMobileAndroid/51.2 lite-443 (Android 4.4.2; SDK 19; x86; unknown Android SDK built for x86; en)";
        private const string VkApiVersion = "5.72";

        
    }
}
