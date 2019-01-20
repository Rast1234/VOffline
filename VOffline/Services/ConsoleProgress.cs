using System;
using System.Threading;
using System.Threading.Tasks;
using ShellProgressBar;
using VOffline.Services.Queues;
using VOffline.Services.Storage;

namespace VOffline.Services
{
    public class ConsoleProgress : IDisposable
    {
        private readonly QueueProvider queueProvider;
        private readonly BackgroundDownloader backgroundDownloader;

        private readonly CancellationTokenSource finishTokenSource;

        private readonly ProgressBar overallProgress;
        private readonly ChildProgressBar jobProgress;
        private readonly ChildProgressBar downloadProgress;


        public ConsoleProgress(QueueProvider queueProvider, BackgroundDownloader backgroundDownloader)
        {
            this.queueProvider = queueProvider;
            this.backgroundDownloader = backgroundDownloader;
            finishTokenSource = new CancellationTokenSource();

            overallProgress = new ProgressBar(0, "total tasks", new ProgressBarOptions()
            {
                CollapseWhenFinished = false
            });
            jobProgress = overallProgress.Spawn(0, "jobs", new ProgressBarOptions()
            {
                CollapseWhenFinished = false
            });
            downloadProgress = overallProgress.Spawn(0, "downloads", new ProgressBarOptions()
            {
                CollapseWhenFinished = false
            });

        }

        public async Task BackgroundUpdate(TimeSpan delay, CancellationToken token)
        {
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(finishTokenSource.Token, token))
            {
                while (!linked.Token.IsCancellationRequested)
                {
                    Update();
                    await Task.Delay(delay, linked.Token).ContinueWith(_ => { }); // do not throw on cancel
                }

                // one last time
                Update();
            }
        }

        private void Update()
        {
            var total = queueProvider.Jobs.Added + queueProvider.Downloads.Added;
            var totalProgress = queueProvider.Jobs.Processed + queueProvider.Downloads.Processed;
            overallProgress.MaxTicks = total;
            overallProgress.Tick(totalProgress, $"total tasks: {totalProgress}/{total}");

            jobProgress.MaxTicks = queueProvider.Jobs.Added;
            jobProgress.Tick(queueProvider.Jobs.Processed, $"jobs: {queueProvider.Jobs}");

            var bytes = backgroundDownloader.ProcessedBytes;
            var humanBytes = FormatBytesHumanReadable(bytes);

            downloadProgress.MaxTicks = queueProvider.Downloads.Added;
            downloadProgress.Tick(queueProvider.Downloads.Processed, $"downloads: {humanBytes} ({bytes} bytes), {queueProvider.Downloads}");
        }

        public void Stop()
        {
            finishTokenSource.Cancel();
        }

        public void Dispose()
        {
            Stop();
            jobProgress?.Dispose();
            downloadProgress?.Dispose();
            overallProgress?.Dispose();
        }

        /// <summary>
        /// Returns the human-readable file size for an arbitrary, 64-bit file size
        /// The default format is "0.### XiB", e.g. "4.2 KiB" or "1.434 GiB" 
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        private static string FormatBytesHumanReadable(long i)
        {
            // Get absolute value
            var absoluteI = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absoluteI >= 0x1000000000000000) // Exabyte
            {
                suffix = "EiB";
                readable = (i >> 50);
            }
            else if (absoluteI >= 0x4000000000000) // Petabyte
            {
                suffix = "PiB";
                readable = (i >> 40);
            }
            else if (absoluteI >= 0x10000000000) // Terabyte
            {
                suffix = "TiB";
                readable = (i >> 30);
            }
            else if (absoluteI >= 0x40000000) // Gigabyte
            {
                suffix = "GiB";
                readable = (i >> 20);
            }
            else if (absoluteI >= 0x100000) // Megabyte
            {
                suffix = "MiB";
                readable = (i >> 10);
            }
            else if (absoluteI >= 0x400) // Kilobyte
            {
                suffix = "KiB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }

            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }
    }
}