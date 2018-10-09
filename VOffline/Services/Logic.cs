using System;
using log4net;
using VOffline.Models;
using VOffline.Models.Vk;
using VOffline.Services.Google;
using VOffline.Services.Vk;

namespace VOffline.Services
{
    public class Logic
    {
        private FileCache<VkCredentials> VkCredentials { get; }
        public TokenMagic TokenMagic { get; }

        public Logic(FileCache<VkCredentials> vkCredentials, TokenMagic tokenMagic)
        {
            VkCredentials = vkCredentials;
            TokenMagic = tokenMagic;
        }

        public void Run(ILog log)
        {
            log.Debug("started");

            if (VkCredentials.Value == null)
            {
                log.Debug("Enter login");
                var login = Console.ReadLine();
                log.Debug("Enter password");
                var password = Console.ReadLine();
                VkCredentials.Value = new VkCredentials
                {
                    Login = login,
                    Password = password
                };
            }

            var vkToken = TokenMagic.GetTokenFromScratch(log);
        }
    }
}