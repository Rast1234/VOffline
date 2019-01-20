using System;
using System.Collections.Async;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Nito.AsyncEx;

namespace VOffline.Services.Queues
{
    public class ParallelWorker<T>
    {
        private int ParallelTasksLimit { get; }
        public int ErrorLimit { get; }
        public ConcurrentQueue<QueueItem<T>> Failed { get; }
        public int Added { get; set; }
        public int Processed { get; set; }
        public int Count => Added - Processed;

        private AsyncCollection<QueueItem<T>> Collection { get; }
        private CancellationTokenSource FinishTokenSource { get; }
        
        private object Lock { get; }

        public ParallelWorker(int parallelTasksLimit, int errorLimit)
        {
            ParallelTasksLimit = parallelTasksLimit;
            ErrorLimit = errorLimit;
            Collection = new AsyncCollection<QueueItem<T>>(new ConcurrentStack<QueueItem<T>>());
            FinishTokenSource = new CancellationTokenSource();
            Failed = new ConcurrentQueue<QueueItem<T>>();
            Lock = new object();
        }

        public async Task Start(QueueItemSelector<T> itemSelector, CancellationToken token, ILog log)
        {
            if (ParallelTasksLimit > 0)
            {
                await EnumerateCollection(log).ParallelForEachAsync((item, i) => EachItem(item, i, itemSelector, token, log), ParallelTasksLimit, token);
            }
            else
            {
                await EnumerateCollection(log).ParallelForEachAsync((item, i) => EachItem(item, i, itemSelector, token, log), token);
            }
        }

        private async Task EachItem(QueueItem<T> item, long i, QueueItemSelector<T> itemSelector, CancellationToken token, ILog log)
        {
            var result = await ProcessItem(item, i, itemSelector, token, log);
            lock (Lock)
            {
                foreach (var x in result)
                {
                    Collection.Add(x, token);
                    Added++;
                }
            }

            lock (Lock)
            {
                Processed++;
                if (Added == Processed && FinishTokenSource.IsCancellationRequested)
                {
                    Collection.CompleteAdding();
                }
            }
        }

        private async Task<IEnumerable<QueueItem<T>>> ProcessItem(QueueItem<T> item, long i, QueueItemSelector<T> itemSelector, CancellationToken token, ILog log)
        {
            // enough async enumerables for now, let it just be simple collection
            var newItems = new List<QueueItem<T>>();
            try
            {
                var result = await itemSelector(item.Data, i, token, log);
                newItems.AddRange(result.Select(x => new QueueItem<T>(x)));
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
                    newItems.Add(item);
                }
            }

            return newItems;
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

        public void CompleteAdding()
        {
            FinishTokenSource.Cancel();

            lock (Lock)
            {
                if (Added == Processed)
                {
                    Collection.CompleteAdding();
                }
            }
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