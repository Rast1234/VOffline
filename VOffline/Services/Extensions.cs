using System.Collections.Generic;
using System.Net;
using log4net;
using RestSharp;
using VOffline.Models.Storage;

namespace VOffline.Services
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

        public static void AddUnique<T>(this HashSet<T> result, IEnumerable<T> items, ILog log)
        {
            foreach (var item in items)
            {
                if (!result.Add(item))
                {
                    log.Warn($"Duplicate item [{item}]");
                }
            }
        }
    }
}