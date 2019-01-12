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
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using VkNet;
using VkNet.Abstractions.Core;
using VkNet.Abstractions.Utils;
using VkNet.Model;
using VkNet.Utils;
using VOffline.Models;
using VOffline.Models.Google;
using VOffline.Models.Vk;
using VOffline.Services;
using VOffline.Services.Google;
using VOffline.Services.Handlers;
using VOffline.Services.Storage;
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
                serviceCollection.AddSingleton<ConstantsProvider>();
                serviceCollection.AddSingleton<VkApi>(_ => CreateVkApi(cts, log));
                serviceCollection.AddSingleton<FilesystemTools>();
                serviceCollection.AddSingleton<DownloadQueueProvider>();
                serviceCollection.AddSingleton<BackgroundDownloader>();
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
            catch (Exception e)
            {
                log.Fatal(e);
                return -1;
            }
        }

        private static VkApi CreateVkApi(CancellationTokenSource cancellationTokenSource, ILog log)
        {
            // VkNet uses its own DI for internal services
            var sc = new ServiceCollection();
            sc.AddSingleton<ConstantsProvider>();
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
            return new VkApi(sc);
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

