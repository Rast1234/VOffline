using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VkNet;
using VkNet.Abstractions.Core;
using VkNet.Abstractions.Utils;
using VkNet.Model;
using VkNet.Model.Attachments;
using VOffline.Models;
using VOffline.Models.Google;
using VOffline.Models.Storage;
using VOffline.Models.Vk;
using VOffline.Services;
using VOffline.Services.Walkers;
using VOffline.Services.Storage;
using VOffline.Services.Token;
using VOffline.Services.Token.Google;
using VOffline.Services.Token.Vk;
using VOffline.Services.Vk;
using VOffline.Services.VkNetHacks;

namespace VOffline
{

    public class Program
    {
        public static async Task Test2(CancellationToken token, ILog log)
        {
            log.Debug("started");
            var l = new List<int> {1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            foreach (var i in l)
            {
                log.Debug($">>> {i}");
                await Task.Delay(TimeSpan.FromSeconds(i), token);
                log.Debug($"<<< {i}");
            }
        }

        public static async Task Test(CancellationToken token, ILog log)
        {
            log.Debug("started");
            //await new AsyncDfsWalk(log).Start(token);
            var pw = new ParallelWalker<int>(20, 2, TestSelector);
            var t = pw.Start(token, log);
            pw.Add(10);
            pw.Add(10);
            pw.Add(10);
            await Task.Delay(TimeSpan.FromSeconds(100), token);
            log.Debug("KEK");
            pw.Add(7);
            pw.Finish();
            await t;
            log.Debug("done");
        }

        private static async Task<IEnumerable<int>> TestSelector(int data, long i, CancellationToken token, ILog log)
        {
            log.Debug($"> {i}: {data}");
            await Task.Delay(TimeSpan.FromSeconds(data));
            if (data < 5)
            {
                log.Debug($"< {i}: {data}: []");
                return new List<int>();
            }

            var result = new List<int> {1, 2, data - 1};
            log.Debug($"< {i}: {data}: {JsonConvert.SerializeObject(result)}");
            return result;
        }

        public static async Task<int> Main(string[] args)
        {
            var log = ConfigureLog4Net();
            var cts = CreateCancellationTokenSource();
            await Test(cts.Token, log);
            Console.ReadLine();
            return 0;

            try
            {
                
                ConfigureJsonSerializer();

                var serviceCollection = new ServiceCollection();

                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddUserSecrets<Settings>()
                    .AddUserSecrets<VkCredentials>()
                    .Build();
                serviceCollection.Configure<Settings>(configuration.GetSection("Settings"));
                serviceCollection.Configure<VkCredentials>(configuration.GetSection("VkCredentials"));

                serviceCollection.AddSingleton<FileCache<VkToken>>();
                serviceCollection.AddSingleton<FileCache<GoogleCredentials>>();
                serviceCollection.AddSingleton<FileCache<GoogleCheckIn>>();
                serviceCollection.AddSingleton<Random>();
                serviceCollection.AddSingleton<GoogleHttpRequests>();
                serviceCollection.AddSingleton<VkHttpRequests>();
                serviceCollection.AddSingleton<VkApiUtils>();
                serviceCollection.AddSingleton<VkApi>(s => CreateVkApi(s, cts, log));
                serviceCollection.AddSingleton<FilesystemTools>();
                serviceCollection.AddSingleton<QueueProvider>();
                serviceCollection.AddSingleton<BackgroundDownloader>();

                serviceCollection.AddSingleton<IWalker<AudioCategory>, AudioWalker>();
                serviceCollection.AddSingleton<IWalker<PhotoCategory>, PhotoWalker>();
                serviceCollection.AddSingleton<IWalker<WallCategory>, WallWalker>();
                serviceCollection.AddSingleton<IWalker<PostComments>, CommentsWalker>();
                serviceCollection.AddSingleton<IWalker<Post>, PostWalker>();
                serviceCollection.AddSingleton<IWalker<Comment>, CommentWalker>();
                serviceCollection.AddSingleton<IWalker<PlaylistWithAudio>, PlaylistWalker>();
                serviceCollection.AddSingleton<IWalker<AlbumWithPhoto>, AlbumWalker>();
                serviceCollection.AddSingleton<AttachmentProcessor>();
                serviceCollection.AddSingleton<IServiceProvider>(s => s);  // hack to avoid circular deps between AttachmentProcessor and Handlers

                serviceCollection.AddTransient(provider => LogManager.GetLogger(Assembly.GetEntryAssembly(), typeof(Program)));
                serviceCollection.AddTransient<AndroidAuth>();
                serviceCollection.AddTransient<VkTokenReceiver>();
                serviceCollection.AddTransient<MTalk>();
                serviceCollection.AddTransient<TokenMagic>();
                serviceCollection.AddTransient<Logic>();

                var services = serviceCollection.BuildServiceProvider();
                await services.GetRequiredService<Logic>().Run(cts.Token, log);
                return 0;
            }
            catch (Exception e)
            {
                log.Fatal(e);
                return -1;
            }
        }

        private static VkApi CreateVkApi(IServiceProvider services, CancellationTokenSource cancellationTokenSource, ILog log)
        {
            // VkNet uses its own DI for internal services
            var sc = new ServiceCollection();
            
            sc.AddSingleton<CancellationTokenSource>(cancellationTokenSource);
            sc.AddSingleton<IRestClient, CustomRestClient>();
            sc.AddSingleton<IAwaitableConstraint, CancellableConstraint>(_ => new CancellableConstraint(3, TimeSpan.FromSeconds(1), cancellationTokenSource.Token));
            sc.AddSingleton<ILoggerFactory, LoggerFactory>();
            sc.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            sc.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.SetMinimumLevel(LogLevel.Warning);
                //builder.AddLog4Net();
                //builder.Services.AddSingleton<ILoggerProvider>((ILoggerProvider)new Log4NetProvider(options));
                builder.AddProvider(new SimpleLoggerProvider(log));
            });

            sc.AddTransient<Settings>(_ => services.GetRequiredService<IOptionsSnapshot<Settings>>().Value);

            var vkApi = new VkApi(sc);
            vkApi.VkApiVersion.SetVersion(5,90);  // hack for linear comments without threads
            return vkApi;
        }

        private static CancellationTokenSource CreateCancellationTokenSource()
        {
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, a) =>
            {
                a.Cancel = true;
                Console.WriteLine("Ctrl-C pressed, trying to stop gracefully...");
                cts.Cancel(true);
            };
            return cts;
        }

        private static void ConfigureJsonSerializer()
        {
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                Converters = new List<JsonConverter>() {new StringEnumConverter()}
            };
        }
        
        private static ILog ConfigureLog4Net()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            return LogManager.GetLogger(Assembly.GetEntryAssembly(), typeof(Program));
        }
    }
}

