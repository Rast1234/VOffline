using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
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
        public static async Task Test(CancellationToken token, ILog log)
        {
            log.Debug("started");
            var r = new Random();
            int i = 0;
            var items = new List<KeyValuePair<int,int>>
            {
                new KeyValuePair<int, int>(1,1000),
                new KeyValuePair<int, int>(1,2000),
                new KeyValuePair<int, int>(1,3000),
                new KeyValuePair<int, int>(1,4000),
                new KeyValuePair<int, int>(1,5000),
                new KeyValuePair<int, int>(1,6000),
            };
            var q = new AsyncQueue<KeyValuePair<int,int>>(items, async (data, t) =>
            {
                return await Task.Run( async () => {
                    await Task.Delay(TimeSpan.FromSeconds(3), t);
                    int count;
                    lock (r)
                    {
                        count = r.Next(0, Math.Max(0, 5 - data.Key));
                        i++;
                    }
                    var result = Enumerable.Repeat(new KeyValuePair<int, int>(data.Key + 1, i), count).ToList();
                    log.Debug($"{data} => {JsonConvert.SerializeObject(result)}");
                    return result;
                }, t);
                
                
            }, 2);
            var task = q.ProcessEverythingAsync(token);
            log.Debug("alala");
            await task;
            log.Debug("done");
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

