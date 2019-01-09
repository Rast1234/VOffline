using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace VOffline.Services
{
    public class Throttler
    {
        public async Task<IReadOnlyList<T>> ProcessWithThrottling<T>(Task<T>[] tasks, int limit, CancellationToken token)
        {
            using (var semaphore = new SemaphoreSlim(limit))
            {
                var newTasks = tasks.Select(async t =>
                {
                    await semaphore.WaitAsync(token);
                    try
                    {
                        var data = await t;
                        return data;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                return await Task.WhenAll(newTasks);
            }
        }

        public async Task ProcessWithThrottling(Task[] tasks, int limit, CancellationToken token)
        {
            using (var semaphore = new SemaphoreSlim(limit))
            {
                var newTasks = tasks.Select(async t =>
                {
                    await semaphore.WaitAsync(token);
                    try
                    {
                        await t;
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                await Task.WhenAll(newTasks);
            }
        }
    }
}