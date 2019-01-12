using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Nito.AsyncEx;
using RestSharp;

namespace VOffline.Services.Handlers
{
    public static class Extensions
    {
        public static void ThrowIfSomethingWrong(this IRestResponse response)
        {
            if (response.ErrorException != null)
            {
                throw new NetworkException("RestSharp failed", response.ErrorException);
            }

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new NetworkException($"Bad response code: [{response.StatusCode}]");
            }

            if (response.RawBytes == null)
            {
                throw new NetworkException($"Null response bytes");
            }
        }
    }
}