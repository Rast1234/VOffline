using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VkNet.Abstractions.Utils;
using VkNet.Utils;
using VOffline.Services.Vk;

namespace VOffline.Services.VkNetHacks
{
    /// <summary>
    /// Retries failed requests, supports cancellation token and custom user agent.
    /// </summary>
    public class CustomRestClient : IRestClient
    {
        private TimeSpan timeoutSeconds;
        private readonly ILogger<RestClient> logger;
        private readonly CancellationToken token;
        private readonly string userAgent;
        private readonly int retryMaxCount;
        private readonly TimeSpan retryDelay;

        public CustomRestClient(ILogger<RestClient> logger, IWebProxy proxy, ConstantsProvider constantsProvider, CancellationTokenSource cancellationTokenSource)
        {
            this.logger = logger;
            this.token = cancellationTokenSource.Token;
            Proxy = proxy;
            userAgent = constantsProvider.UserAgent;
            retryMaxCount = constantsProvider.RequestRetryCount;
            retryDelay = constantsProvider.RequestRetryDelay;
        }

        public IWebProxy Proxy { get; set; }

        public TimeSpan Timeout
        {
            get => timeoutSeconds == TimeSpan.Zero ? TimeSpan.FromSeconds(300) : timeoutSeconds;
            set => timeoutSeconds = value;
        }

        public Task<HttpResponse<string>> GetAsync(Uri uri,IEnumerable<KeyValuePair<string, string>> parameters)
        {
            var queries = parameters
                .Where(parameter => !string.IsNullOrWhiteSpace(parameter.Value))
                .Select(parameter => $"{parameter.Key.ToLowerInvariant()}={parameter.Value}");
            var url = new UriBuilder(uri)
            {
                Query = string.Join("&", queries)
            };
            logger?.LogDebug($"GET request: {url.Uri}");
            var request = new HttpRequestMessage(HttpMethod.Get, uri);
            return CallWithRetry(httpClient => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token));
        }

        public Task<HttpResponse<string>> PostAsync(Uri uri,IEnumerable<KeyValuePair<string, string>> parameters)
        {
            logger?.LogDebug($"POST request: {uri}{Environment.NewLine}{Utilities.PrettyPrintJson(JsonConvert.SerializeObject(parameters))}");
            var content = new FormUrlEncodedContent(parameters);
            var request = new HttpRequestMessage(HttpMethod.Post, uri) { Content = content };
            return CallWithRetry(httpClient => httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token));
        }

        private async Task<HttpResponse<string>> CallWithRetry(Func<HttpClient, Task<HttpResponseMessage>> method)
        {
            var errors = new Lazy<List<Exception>>();
            for (var i = 1; i <= retryMaxCount; i++)
            {
                try
                {
                    var result = await Call(method);
                    // inspect for bad responses. yes this is wrong to do it here but so convenient!
                    var json = result.Message ?? result.Value;
                    VkErrors.IfErrorThrowException(json);
                    return result;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    errors.Value.Add(e);
                    logger?.LogWarning($"Failed vk request, attempt {i}/{retryMaxCount}", e);
                }

                await Task.Delay(retryDelay, token);
            }
            logger?.LogError($"Failed vk request after {retryMaxCount} attempts");
            throw new Exception(string.Join("\n---------------------------------\n", errors.Value.Select(x => x.ToString())));
        }

        private async Task<HttpResponse<string>> Call(Func<HttpClient, Task<HttpResponseMessage>> method)
        {
            var useProxyCondition = Proxy != null;

            if (useProxyCondition)
            {
                logger?.LogDebug($"Use Proxy: {Proxy}");
            }
            var handler = new HttpClientHandler
            {
                Proxy = Proxy,
                UseProxy = useProxyCondition
            };

            using (var client = new HttpClient(handler) { Timeout = Timeout })
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
                var response = await method(client).ConfigureAwait(false);
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                logger?.LogDebug($"Response:{Environment.NewLine}{Utilities.PrettyPrintJson(content)}");
                var url = response.RequestMessage.RequestUri.ToString();
                return response.IsSuccessStatusCode
                    ? HttpResponse<string>.Success(response.StatusCode, content, url)
                    : HttpResponse<string>.Fail(response.StatusCode, content, url);
            }
        }
    }
}
