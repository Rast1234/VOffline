using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using VOffline.Services;

namespace VOffline.Models.Storage
{
    public class Download : IDownload
    {
        public Download(Uri uri, DirectoryInfo location, string desiredName)
        {
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            Location = location ?? throw new ArgumentNullException(nameof(location));
            DesiredName = desiredName ?? throw new ArgumentNullException(nameof(desiredName));
        }

        public Uri Uri { get; }
        public DirectoryInfo Location { get; }
        public string DesiredName { get; }

        public async Task<byte[]> GetContent(CancellationToken token)
        {
            var client = new RestClient($"{Uri.Scheme}://{Uri.Authority}");
            var response = await client.ExecuteGetTaskAsync(new RestRequest(Uri.PathAndQuery), token);
            response.ThrowIfSomethingWrong();
            return response.RawBytes;
        }

        public override string ToString()
        {
            return $"[{Location.FullName}][{DesiredName}] ({Uri})";
        }
    }
}