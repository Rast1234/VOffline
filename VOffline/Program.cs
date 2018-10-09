using System;
using System.IO;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.Extensions.DependencyInjection;
using VOffline.Models;
using VOffline.Models.Google;
using VOffline.Models.Vk;
using VOffline.Services;
using VOffline.Services.Google;
using VOffline.Services.Vk;

namespace VOffline
{
    public class Program
    {
        static void Main(string[] args)
        {
            var log = ConfigureLog4Net();
            try
            {
                var serviceCollection = new ServiceCollection();

                serviceCollection.AddTransient(provider => LogManager.GetLogger(Assembly.GetEntryAssembly(), typeof(Program)));

                serviceCollection.AddSingleton<FileCache<VkCredentials>>();
                serviceCollection.AddSingleton<FileCache<VkToken>>();
                serviceCollection.AddSingleton<FileCache<GoogleCredentials>>();
                serviceCollection.AddSingleton<FileCache<GoogleCheckIn>>();
                serviceCollection.AddSingleton<Random>();
                serviceCollection.AddSingleton<GoogleHttpRequests>();
                serviceCollection.AddSingleton<VkHttpRequests>();

                serviceCollection.AddTransient<AndroidAuth>();
                serviceCollection.AddTransient<VkTokenReceiver>();
                serviceCollection.AddTransient<MTalk>();
                serviceCollection.AddTransient<TokenMagic>();
                serviceCollection.AddTransient<Logic>();

                var services = serviceCollection.BuildServiceProvider();
                services.GetRequiredService<Logic>().Run(log);
            }
            catch (Exception e)
            {
                log.Fatal(e);
            }
            Console.ReadLine();
        }

        private static ILog ConfigureLog4Net()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
            return LogManager.GetLogger(Assembly.GetEntryAssembly(), typeof(Program));
        }
    }
}

