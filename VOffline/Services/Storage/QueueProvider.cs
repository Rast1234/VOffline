using System;
using System.Collections.Async;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Nito.AsyncEx;
using VkNet.Abstractions.Category;
using VkNet.Model.RequestParams;
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


    // enough async enumerables for now, let it just be list of new items
    public delegate Task<IEnumerable<TData>> QueueItemSelector<TData>(TData data, long i, CancellationToken token, ILog log);


    public class ParallelWalker<T>
    {
        private int ParallelTasksLimit { get; }
        public int ErrorLimit { get; }
        public QueueItemSelector<T> ItemSelector { get; }
        private AsyncCollection<QueueItem<T>> Collection { get; }
        private ConcurrentQueue<QueueItem<T>> Failed { get; }
        private CancellationTokenSource FinishTokenSource { get; }
        private int Added { get; set; }
        private int Processed { get; set; }
        private object Lock { get; }

        public ParallelWalker(int parallelTasksLimit, int errorLimit, QueueItemSelector<T> itemSelector)
        {
            ParallelTasksLimit = parallelTasksLimit;
            ErrorLimit = errorLimit;
            ItemSelector = itemSelector;
            Collection = new AsyncCollection<QueueItem<T>>(new ConcurrentStack<QueueItem<T>>());
            FinishTokenSource = new CancellationTokenSource();
            Failed = new ConcurrentQueue<QueueItem<T>>();
            Lock = new object();
        }

        public async Task Start(CancellationToken token, ILog log)
        {
            await EnumerateCollection(log).ParallelForEachAsync((item, i) => EachItem(item, i, token, log), ParallelTasksLimit, token);
        }

        private async Task EachItem(QueueItem<T> item, long i, CancellationToken token, ILog log)
        {
            var result = await ItemSelector(item.Data, i, token, log);
            log.Warn("! enter L1");
            lock (Lock)
            {
                foreach (var x in result)
                {
                    var newItem = new QueueItem<T>(x);
                    Collection.Add(newItem, token);
                    Added++;
                }
            }
            log.Warn("! leave L1");

            /*
            await ProcessItem(item, i, log).ParallelForEachAsync(x =>
            {
                lock (Lock)
                {
                    Collection.Add(x, token);
                    Added++;
                }

                return Task.CompletedTask;
            }, 3, token);
            */
            log.Warn("! enter L2");
            lock (Lock)
            {
                Processed++;
                 if (Added == Processed && FinishTokenSource.IsCancellationRequested)
                {
                    Collection.CompleteAdding();
                }
            }
            log.Warn("! leave L2");
        }

        private IAsyncEnumerator<QueueItem<T>> ProcessItem(QueueItem<T> item, long i, ILog log)
        {
            return new AsyncEnumerator<QueueItem<T>>(async yield =>
            {
                // TODO: some actual work here
                try
                {
                    var result = await ItemSelector(item.Data, i, yield.CancellationToken, log);
                    foreach (var x in result)
                    {
                        var newItem = new QueueItem<T>(x);
                        await yield.ReturnAsync(newItem);
                    }
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
                        Failed.Enqueue(item);
                    }
                    else
                    {
                        await yield.ReturnAsync(item);
                    }
                }
            });
        }

        public void Add(T data)
        {
            lock (Lock)
            {
                FinishTokenSource.Token.ThrowIfCancellationRequested();
                var item = new QueueItem<T>(data);
                Collection.Add(item);
                Added++;
            }
            
        }

        public void Finish()
        {
            FinishTokenSource.Cancel();
        }

        private IAsyncEnumerator<QueueItem<T>> EnumerateCollection(ILog log)
        {
            return new AsyncEnumerator<QueueItem<T>>(async yield =>
            {
                var x = await GetItem(yield.CancellationToken);
                while (x != null)
                {
                    await yield.ReturnAsync(x);
                    x = await GetItem(yield.CancellationToken);
                }
            });
        }

        private async Task<QueueItem<T>> GetItem(CancellationToken token)
        {
            try
            {
                return await Collection.TakeAsync(token);
            }
            catch (InvalidOperationException)
            {
                // thrown if finished while waiting for item
                return null;
            }
        }
    }
}