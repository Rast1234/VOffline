using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RestSharp;
using VkNet;
using VOffline.Services.Handlers;

namespace VOffline.Models.Storage
{
    public interface IDownload
    {
        DirectoryInfo Location { get; }
        string DesiredName { get; }
        int RetryCount { get; }
        IReadOnlyList<Exception> Errors { get; }
        void AddError(Exception e);
        Task<byte[]> GetContent(VkApi vkApi, CancellationToken token);
    }

    public class Download : IDownload
    {
        private readonly List<Exception> errors;

        public Download(Uri uri, DirectoryInfo location, string desiredName)
        {
            errors = new List<Exception>();
            Uri = uri ?? throw new ArgumentNullException(nameof(uri));
            Location = location ?? throw new ArgumentNullException(nameof(location));
            DesiredName = desiredName ?? throw new ArgumentNullException(nameof(desiredName));
        }

        public Uri Uri { get; }
        public DirectoryInfo Location { get; }
        public string DesiredName { get; }

        public int RetryCount => errors.Count;

        public IReadOnlyList<Exception> Errors => errors;

        public void AddError(Exception e)
        {
            errors.Add(e);
        }

        public async Task<byte[]> GetContent(VkApi vkApi, CancellationToken token)
        {
            var client = new RestClient($"{Uri.Scheme}://{Uri.Authority}");
            var response = await client.ExecuteGetTaskAsync(new RestRequest(Uri.PathAndQuery), token);
            response.ThrowIfSomethingWrong();
            return response.RawBytes;
        }
    }

    public class LyricsVkDownload : IDownload
    {
        private readonly long lyricsId;
        private readonly List<Exception> errors;

        public LyricsVkDownload(long lyricsId, DirectoryInfo location, string desiredName)
        {
            this.lyricsId = lyricsId;
            errors = new List<Exception>();
            Location = location ?? throw new ArgumentNullException(nameof(location));
            DesiredName = desiredName ?? throw new ArgumentNullException(nameof(desiredName));
        }

        public DirectoryInfo Location { get; }
        public string DesiredName { get; }

        public int RetryCount => errors.Count;

        public IReadOnlyList<Exception> Errors => errors;

        public void AddError(Exception e)
        {
            errors.Add(e);
        }

        public async Task<byte[]> GetContent(VkApi vkApi, CancellationToken token)
        {
            var lyrics = await vkApi.Audio.GetLyricsAsync(lyricsId);
            return !string.IsNullOrWhiteSpace(lyrics?.Text)
                ? new byte[] { }  // TODO: fix this to provide a no-value without exceptions
                : Encoding.UTF8.GetBytes(lyrics.Text);
        }
    }
}