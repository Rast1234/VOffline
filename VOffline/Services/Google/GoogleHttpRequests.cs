using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using RestSharp;
using VOffline.Models.Google;

namespace VOffline.Services.Google
{
    public class GoogleHttpRequests
    {
        private FileCache<GoogleCredentials> CredentialsCache { get; }
        private FileCache<GoogleCheckIn> CheckInCache { get; }
        public string RandomAppId { get; }

        public GoogleHttpRequests(FileCache<GoogleCredentials> credentialsCache, FileCache<GoogleCheckIn> checkInCache, Random random)
        {
            CredentialsCache = credentialsCache;
            CheckInCache = checkInCache;
            RandomAppId = GenerateRandomString(11, random);
        }

        public byte[] GetCheckIn(ILog log)
        {
            if (CheckInCache.Value != null)
            {
                return CheckInCache.Value.Response;
            }

            log.Debug($"request check in");
            var response = RequestCheckIn();
            CheckInCache.Value = new GoogleCheckIn()
            {
                Response = response
            };
            return CheckInCache.Value.Response;
        }

        public string GetReceipt(ILog log)
        {
            var client = GetClient("https://android.clients.google.com/c2dm/register3");
            log.Debug($"request receipt 1");
            RequestReceipt1(client);
            log.Debug($"request receipt 2");
            return RequestReceipt2(client);
        }

        private byte[] RequestCheckIn()
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("Expect", "");
            request.AddParameter("application/x-protobuffer", queryMessage, ParameterType.RequestBody);
            var client = GetClient("https://android.clients.google.com/checkin");
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException($"{nameof(RequestCheckIn)} bad response: {response.StatusCode}\n{response.Content}");
            }
            return response.RawBytes;
        }

        private void RequestReceipt1(RestClient client)
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", $"AidLogin {CredentialsCache.Value.Id}:{CredentialsCache.Value.Token}");
            foreach (var requestParam in GetRequestParams())
            {
                request.AddParameter(requestParam.Key, requestParam.Value, ParameterType.GetOrPost);
            }

            var response = client.Execute(request);  // result ignored?
            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException($"{nameof(RequestReceipt1)} bad response: {response.StatusCode}\n{response.Content}");
            }
        }

        private string RequestReceipt2(RestClient client)
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("Authorization", $"AidLogin {CredentialsCache.Value.Id}:{CredentialsCache.Value.Token}");
            var requestParams = GetRequestParams();
            requestParams["X-scope"] = $"id{string.Empty}";  // id is always empty here?
            requestParams["X-kid"] = "|ID|2|";
            requestParams["X-X-kid"] = "|ID|2|";
            foreach (var requestParam in requestParams)
            {
                request.AddParameter(requestParam.Key, requestParam.Value, ParameterType.GetOrPost);
            }
            var response = client.Execute(request);
            if (!response.IsSuccessful)
            {
                throw new InvalidOperationException($"{nameof(RequestReceipt2)} bad response: {response.StatusCode}\n{response.Content}");
            }

            var result = response.Content.Split("|ID|2|:")[1];
            if (result == "PHONE_REGISTRATION_ERROR")
            {
                throw new InvalidOperationException($"{nameof(RequestReceipt2)} bad response: {result}\n{response.Content}");
            }

            return result;
        }

        private Dictionary<string, string> GetRequestParams()
        {
            return new Dictionary<string, string>
            {
                {"X-scope", "GCM"},
                {"X-osv", "23"},
                {"X-subtype", "54740537194"},
                {"X-app_ver", "443"},
                {"X-kid", "|ID|1|"},
                {"X-appid", RandomAppId},
                {"X-gmsv", "13283005"},
                {"X-cliv", "iid-10084000"},
                {"X-app_ver_name", "51.2 lite"},
                {"X-X-kid", "|ID|1|"},
                {"X-subscription", "54740537194"},
                {"X-X-subscription", "54740537194"},
                {"X-X-subtype", "54740537194"},
                {"app", "com.perm.kate_new_6"},
                {"sender", "54740537194"},
                {"device", CredentialsCache.Value.Id.ToString()},
                {"cert", "966882ba564c2619d55d0a9afd4327a38c327456"},
                {"app_ver", "443"},
                {"info", "g57d5w1C4CcRUO6eTSP7b7VoT8yTYhY"},
                {"gcm_ver", "13283005"},
                {"plat", "0"},
                {"X-messenger2", "1"}
            };
        }

        private RestClient GetClient(string url)
        {
            var client = new RestClient(url)
            {
                UserAgent = GcmUserAgent
            };
            client.RemoteCertificateValidationCallback += (sender, certificate, chain, errors) => true;
            return client;
        }

        private string GenerateRandomString(int length, Random random)
        {
            var sb = new StringBuilder(length);
            for (var i = 0; i < length; i++)
            {
                sb.Append(alphabet[random.Next(alphabet.Length)]);
            }
            return sb.ToString();
        }

        private const string GcmUserAgent = "Android-GCM/1.5 (generic_x86 KK)";

        private static byte[] queryMessage = {
            0x10, 0x00, 0x1a, 0x2a, 0x31, 0x2d, 0x39, 0x32, 0x39, 0x61, 0x30, 0x64, 0x63, 0x61, 0x30, 0x65, 0x65, 0x65, 0x35, 0x35, 0x35, 0x31, 0x33, 0x32, 0x38, 0x30, 0x31, 0x37, 0x31, 0x61, 0x38, 0x35, 0x38, 0x35, 0x64, 0x61, 0x37, 0x64, 0x63, 0x64, 0x33, 0x37, 0x30, 0x30, 0x66, 0x38, 0x22, 0xe3, 0x01, 0x0a, 0xbf, 0x01, 0x0a, 0x45, 0x67, 0x65, 0x6e, 0x65, 0x72, 0x69, 0x63, 0x5f, 0x78, 0x38, 0x36, 0x2f, 0x67, 0x6f, 0x6f, 0x67, 0x6c, 0x65, 0x5f, 0x73, 0x64, 0x6b, 0x5f, 0x78, 0x38, 0x36, 0x2f, 0x67, 0x65, 0x6e, 0x65, 0x72, 0x69, 0x63, 0x5f, 0x78, 0x38, 0x36, 0x3a, 0x34, 0x2e, 0x34, 0x2e, 0x32, 0x2f, 0x4b, 0x4b, 0x2f, 0x33, 0x30, 0x37, 0x39, 0x31, 0x38, 0x33, 0x3a, 0x65, 0x6e, 0x67, 0x2f, 0x74, 0x65, 0x73, 0x74, 0x2d, 0x6b, 0x65, 0x79, 0x73, 0x12, 0x06, 0x72, 0x61, 0x6e, 0x63, 0x68, 0x75, 0x1a, 0x0b, 0x67, 0x65, 0x6e, 0x65, 0x72, 0x69, 0x63, 0x5f, 0x78, 0x38, 0x36, 0x2a, 0x07, 0x75, 0x6e, 0x6b, 0x6e, 0x6f, 0x77, 0x6e, 0x32, 0x0e, 0x61, 0x6e, 0x64, 0x72, 0x6f, 0x69, 0x64, 0x2d, 0x67, 0x6f, 0x6f, 0x67, 0x6c, 0x65, 0x40, 0x85, 0xb5, 0x86, 0x06, 0x4a, 0x0b, 0x67, 0x65, 0x6e, 0x65, 0x72, 0x69, 0x63, 0x5f, 0x78, 0x38, 0x36, 0x50, 0x13, 0x5a, 0x19, 0x41, 0x6e, 0x64, 0x72, 0x6f, 0x69, 0x64, 0x20, 0x53, 0x44, 0x4b, 0x20, 0x62, 0x75, 0x69, 0x6c, 0x74, 0x20, 0x66, 0x6f, 0x72, 0x20, 0x78, 0x38, 0x36, 0x62, 0x07, 0x75, 0x6e, 0x6b, 0x6e, 0x6f, 0x77, 0x6e, 0x6a, 0x0e, 0x67, 0x6f, 0x6f, 0x67, 0x6c, 0x65, 0x5f, 0x73, 0x64, 0x6b, 0x5f, 0x78, 0x38, 0x36, 0x70, 0x00, 0x10, 0x00, 0x32, 0x06, 0x33, 0x31, 0x30, 0x32, 0x36, 0x30, 0x3a, 0x06, 0x33, 0x31, 0x30, 0x32, 0x36, 0x30, 0x42, 0x0b, 0x6d, 0x6f, 0x62, 0x69, 0x6c, 0x65, 0x3a, 0x4c, 0x54, 0x45, 0x3a, 0x48, 0x00, 0x32, 0x05, 0x65, 0x6e, 0x5f, 0x55, 0x53, 0x38, 0xf0, 0xb4, 0xdf, 0xa6, 0xb9, 0x9a, 0xb8, 0x83, 0x8e, 0x01, 0x52, 0x0f, 0x33, 0x35, 0x38, 0x32, 0x34, 0x30, 0x30, 0x35, 0x31, 0x31, 0x31, 0x31, 0x31, 0x31, 0x30, 0x5a, 0x00, 0x62, 0x10, 0x41, 0x6d, 0x65, 0x72, 0x69, 0x63, 0x61, 0x2f, 0x4e, 0x65, 0x77, 0x5f, 0x59, 0x6f, 0x72, 0x6b, 0x70, 0x03, 0x7a, 0x1c, 0x37, 0x31, 0x51, 0x36, 0x52, 0x6e, 0x32, 0x44, 0x44, 0x5a, 0x6c, 0x31, 0x7a, 0x50, 0x44, 0x56, 0x61, 0x61, 0x65, 0x45, 0x48, 0x49, 0x74, 0x64, 0x2b, 0x59, 0x67, 0x3d, 0xa0, 0x01, 0x00, 0xb0, 0x01, 0x00
        };

        private const string alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ_-";
    }
}
