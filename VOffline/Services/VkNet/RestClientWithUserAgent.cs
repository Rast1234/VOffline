using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VkNet.Abstractions.Utils;
using VkNet.Utils;

namespace VOffline.Services.Vk
{
    public class RestClientWithUserAgent : IRestClient
    {
        /// <summary>The log</summary>
        private readonly ILogger<RestClient> _logger;

        private TimeSpan _timeoutSeconds;
        private string userAgent;

        /// <inheritdoc />
        public RestClientWithUserAgent(ILogger<RestClient> logger, IWebProxy proxy, ConstantsProvider constantsProvider)
        {
            _logger = logger;
            Proxy = proxy;
            userAgent = constantsProvider.UserAgent;
        }

        /// <inheritdoc />
        public IWebProxy Proxy { get; set; }

        /// <inheritdoc />
        public TimeSpan Timeout
        {
            get
            {
                if (!(this._timeoutSeconds == TimeSpan.Zero))
                    return this._timeoutSeconds;
                return TimeSpan.FromSeconds(300.0);
            }
            set
            {
                this._timeoutSeconds = value;
            }
        }

        public Task<HttpResponse<string>> GetAsync(Uri uri,IEnumerable<KeyValuePair<string, string>> parameters)
        {
            IEnumerable<string> values = parameters.Where<KeyValuePair<string, string>>((Func<KeyValuePair<string, string>, bool>)(parameter => !string.IsNullOrWhiteSpace(parameter.Value))).Select<KeyValuePair<string, string>, string>((Func<KeyValuePair<string, string>, string>)(parameter => parameter.Key.ToLowerInvariant() + "=" + parameter.Value));
            UriBuilder uriBuilder = new UriBuilder(uri)
            {
                Query = string.Join("&", values)
            };
            ILogger<RestClient> logger = this._logger;
            if (logger != null)
                logger.LogDebug(string.Format("GET request: {0}", (object)uriBuilder.Uri));
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, uri);
            return this.Call((Func<HttpClient, Task<HttpResponseMessage>>)(httpClient => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)));
        }

        public Task<HttpResponse<string>> PostAsync(Uri uri,IEnumerable<KeyValuePair<string, string>> parameters)
        {
            if (this._logger != null)
            {
                string json = JsonConvert.SerializeObject((object)parameters);
                this._logger.LogDebug(string.Format("POST request: {0}{1}{2}", (object)uri, (object)Environment.NewLine, (object)Utilities.PreetyPrintJson(json)));
            }
            FormUrlEncodedContent urlEncodedContent = new FormUrlEncodedContent(parameters);
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri)
            {
                Content = (HttpContent)urlEncodedContent
            };
            return this.Call((Func<HttpClient, Task<HttpResponseMessage>>)(httpClient => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead)));
        }

        private async Task<HttpResponse<string>> Call(Func<HttpClient, Task<HttpResponseMessage>> method)
        {
            var httpClientHandler = new HttpClientHandler
            {
                UseProxy = false
            };
            if (Proxy != null)
            {
                httpClientHandler = new HttpClientHandler
                {
                    Proxy = Proxy,
                    UseProxy = true
                };
                var logger = _logger;
                logger?.LogDebug($"Use Proxy: {(object) Proxy}");
            }

            using (var client = new HttpClient(httpClientHandler))
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                if (Timeout != TimeSpan.Zero)
                    client.Timeout = Timeout;
                var response = await method(client).ConfigureAwait(false);
                var requestUri = response.RequestMessage.RequestUri.ToString();
                if (!response.IsSuccessStatusCode)
                    return HttpResponse<string>.Fail(response.StatusCode,
                        await response.Content.ReadAsStringAsync().ConfigureAwait(false), requestUri);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var logger = _logger;
                logger?.LogDebug("Response:" + Environment.NewLine + Utilities.PreetyPrintJson(json));
                return HttpResponse<string>.Success(response.StatusCode, json, requestUri);
            }
        }
    }

    

}