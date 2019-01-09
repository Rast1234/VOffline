using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using log4net;

namespace VOffline.Services
{
    public class Retrier
    {
        public static async Task<T> Retry<T>(Func<Task<T>> taskFactory, int limit, TimeSpan delay, CancellationToken token, ILog log)
        {
            var exceptions = new List<Exception>();
            for (var i = 1; i <= limit; i++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    return await taskFactory();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
                log.Warn($"Attempt {i}/{limit}, last error was [{exceptions.LastOrDefault()?.GetType()}]");
                await Task.Delay(delay, token);
            }
            throw new AggregateException(exceptions);
        }
    }
}