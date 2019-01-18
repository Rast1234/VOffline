using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;
using VOffline.Models;
using VOffline.Models.Storage;

namespace VOffline.Services.Storage
{
    public class QueueProvider
    {
        public QueueProvider(IOptionsSnapshot<Settings> settings)
        {
            var queueSizeLimit = settings.Value.DownloadQueueLimit;
            Pending = queueSizeLimit > 0
                ? new AsyncProducerConsumerQueue<IDownload>(settings.Value.DownloadQueueLimit)
                : new AsyncProducerConsumerQueue<IDownload>();

        }

        public async Task EnqueueAllDownloads(IEnumerable<IDownload> items, CancellationToken token)
        {
            await Task.WhenAll(items.Select(async x => await Pending.EnqueueAsync(x, token)));
        }

        public AsyncProducerConsumerQueue<IDownload> Pending { get; }
    }

    public class QueueItem<TData>
    {
        public QueueItem(TData data)
        {
            Data = data;
            Errors = new Lazy<List<Exception>>();
        }

        public TData Data { get; }
        public Lazy<List<Exception>> Errors { get; }
    }

    public delegate Task<IEnumerable<TData>> QueueItemProcessor<TData>(TData data, CancellationToken token);

    public class AsyncSemaphoredCollection<T>
    {
        public AsyncSemaphoredCollection()
        {
            InnerCollection = new ConcurrentStack<T>();
            Semaphore = new SemaphoreSlim(0);
            FinishTokenSource = new CancellationTokenSource();
        }

        public int Count => InnerCollection.Count;

        private CancellationTokenSource FinishTokenSource { get; set; }

        private SemaphoreSlim Semaphore { get; }

        private ConcurrentStack<T> InnerCollection { get; }

        public async Task<T> Get(CancellationToken token)
        {
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(FinishTokenSource.Token, token))
            {
                using (await Semaphore.LockAsync(linkedCts.Token).AsTask())
                {
                    if (!InnerCollection.TryPop(out var data))
                    {
                        throw new InvalidOperationException($"InnerCollection is empty");
                    }

                    return data;
                }
            }
        }

        public void Add(T data)
        {
            FinishTokenSource.Token.ThrowIfCancellationRequested();
            InnerCollection.Push(data);
            Semaphore.Release(1);
        }

        public void Finish()
        {
            FinishTokenSource.Cancel();
        }
    }

    public class AsyncQueue<TData>
    {
        public AsyncQueue(IEnumerable<TData> initialData, QueueItemProcessor<TData> processor, int errorLimit)
        {
            var queueData = initialData.Select(d => new QueueItem<TData>(d));
            Pending = new AsyncSemaphoredCollection<QueueItem<TData>>();
            foreach (var queueItem in queueData)
            {
                Pending.Add(queueItem);
            }

            Running = new ConcurrentDictionary<QueueItem<TData>, byte>();
            Failed = new ConcurrentDictionary<QueueItem<TData>, DateTime>();
            ErrorLimit = errorLimit;
            Semaphore = new SemaphoreSlim(1, 1);
            Processor = processor;
        }


        private AsyncSemaphoredCollection<QueueItem<TData>> Pending { get; }
        private ConcurrentDictionary<QueueItem<TData>, byte> Running { get; }
        private ConcurrentDictionary<QueueItem<TData>, DateTime> Failed { get; }
        private SemaphoreSlim Semaphore { get; }
        private int ErrorLimit { get; }
        private QueueItemProcessor<TData> Processor { get; }

        public async Task ProcessEverythingAsync(CancellationToken token)
        {
            /*
            TODO: 2 semaphores:
                * Modification semaphore between collections. single wait/release.
                * Dequeue semaphore. release N on add N items, wait on dequeue and also wait on modification semaphore
             */
            while (!IsEmpty())
            {
                var item = await Dequeue(token);
                await ProcessItemAsync(item, token);
            }
        }

        private async Task ProcessItemAsync(QueueItem<TData> item, CancellationToken token)
        {
            try
            {
                var result = await Processor(item.Data, token);
                foreach (var newData in result)
                {
                    var newItem = new QueueItem<TData>(newData);
                    await Enqueue(newItem, token);
                }

                await Success(item, token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                item.Errors.Value.Add(e);
                if (item.Errors.Value.Count >= ErrorLimit)
                {
                    await Fail(item, token);
                }
                else
                {
                    await Requeue(item, token);
                }
            }

        }

        private async Task Enqueue(QueueItem<TData> item, CancellationToken token)
        {
            await Semaphore.WaitAsync(token);
            Pending.Add(item);
            Semaphore.Release();
        }

        private async Task<QueueItem<TData>> Dequeue(CancellationToken token)
        {
            var item = await Pending.Get(token);
            await Semaphore.WaitAsync(token);
            if (!Running.TryAdd(item, 0))
            {
                throw new InvalidOperationException($"Duplicate running item");
            }

            Semaphore.Release();
            return item;
        }

        private async Task Success(QueueItem<TData> item, CancellationToken token)
        {
            await Semaphore.WaitAsync(token);
            if (!Running.TryRemove(item, out var _))
            {
                throw new InvalidOperationException($"Running item lost");
            }
            Semaphore.Release();
        }

        private async Task Fail(QueueItem<TData> item, CancellationToken token)
        {
            await Semaphore.WaitAsync(token);
            if (!Running.TryRemove(item, out var _))
            {
                throw new InvalidOperationException($"Running item lost");
            }

            if (!Failed.TryAdd(item, DateTime.Now))
            {
                throw new InvalidOperationException($"Duplicate failed item");
            }
            Semaphore.Release();
        }

        private async Task Requeue(QueueItem<TData> item, CancellationToken token)
        {
            await Semaphore.WaitAsync(token);
            if (!Running.TryRemove(item, out var _))
            {
                throw new InvalidOperationException($"Running item lost");
            }

            Pending.Add(item);
            Semaphore.Release();
        }

        private bool IsEmpty()
        {
            if (Pending.Count != 0 || Running.Count != 0)
            {
                return false;
            }

            Pending.Finish();
            return true;
        }
    }
}