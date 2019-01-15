using System;
using System.Collections.Generic;
using System.IO;
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
using VOffline.Models;
using VOffline.Models.Google;
using VOffline.Models.Vk;
using VOffline.Services;
using VOffline.Services.Handlers;
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
        public static async Task<int> Main(string[] args)
        {
            var log = ConfigureLog4Net();

            try
            {
                var cts = CreateCancellationTokenSource();
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
                serviceCollection.AddSingleton<DownloadQueueProvider>();
                serviceCollection.AddSingleton<BackgroundDownloader>();

                serviceCollection.AddSingleton<WallHandler>();
                serviceCollection.AddSingleton<PostHandler>();
                serviceCollection.AddSingleton<CommentsHandler>();
                serviceCollection.AddSingleton<CommentHandler>();
                serviceCollection.AddSingleton<AudioHandler>();
                serviceCollection.AddSingleton<PlaylistHandler>();
                serviceCollection.AddSingleton<PhotoHandler>();
                serviceCollection.AddSingleton<AlbumHandler>();
                serviceCollection.AddSingleton<AttachmentProcessor>();

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
            catch (TaskCanceledException)
            {
                log.Warn($"Canceled by user");
                return -2;
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

