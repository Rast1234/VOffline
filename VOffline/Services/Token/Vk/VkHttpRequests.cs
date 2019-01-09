using System;
using log4net;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using VOffline.Models.Vk;
using RestClient = RestSharp.RestClient;

namespace VOffline.Services.Vk
{
    public class VkHttpRequests
    {
        private readonly string userAgent;
        private VkCredentials VkCredentials { get; }

        public VkHttpRequests(IOptionsSnapshot<VkCredentials> vkCredentials, UserAgentProvider userAgentProvider)
        {
            userAgent = userAgentProvider.UserAgent;
            VkCredentials = vkCredentials.Value;
        }

        public string GetNonRefreshedToken(ILog log)
        {
            var client = GetClient("https://oauth.vk.com/token");
            var request = new RestRequest(Method.GET);
            request.AddQueryParameter("grant_type", "password");
            request.AddQueryParameter("client_id", ClientId.ToString());
            request.AddQueryParameter("client_secret", "lxhD8OD7dMsqtXIm5IUY");
            request.AddQueryParameter("username", VkCredentials.Login);
            request.AddQueryParameter("password", VkCredentials.Password);
            request.AddQueryParameter("v", VkApiVersion);
            request.AddQueryParameter("scope", Scope);
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
                UserAgent = userAgent
            };
            client.RemoteCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            return client;
        }

        

        public const string VkApiVersion = "5.72";
        public const ulong ClientId = 2685278;
        public const string Scope = "audio,offline,notify,friends,photos,video,stories,pages,status,notes,messages,wall,ads,docs,groups,notifications,stats,email,market";
    }
}
