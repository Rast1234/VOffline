﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using VOffline.Models;
using VOffline.Models.Storage;

namespace VOffline.Services.Storage
{
    public class DownloadQueueProvider
    {
        public DownloadQueueProvider(IOptionsSnapshot<Settings> settings)
        {
            var queueSizeLimit = settings.Value.DownloadQueueLimit;
            Pending = queueSizeLimit > 0
                ? new AsyncProducerConsumerQueue<IDownload>(settings.Value.DownloadQueueLimit)
                : new AsyncProducerConsumerQueue<IDownload>();
        }

        public async Task EnqueueAll(IEnumerable<IDownload> items, CancellationToken token)
        {
            await Task.WhenAll(items.Select(async x => await Pending.EnqueueAsync(x, token)));
        }

        public AsyncProducerConsumerQueue<IDownload> Pending { get; }
    }
}