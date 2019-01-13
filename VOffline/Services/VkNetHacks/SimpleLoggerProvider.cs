using log4net;
using Microsoft.Extensions.Logging;

namespace VOffline.Services.VkNetHacks
{
    public class SimpleLoggerProvider : ILoggerProvider
    {
        private readonly SimpleLogger logger;

        public SimpleLoggerProvider(ILog log)
        {
            logger = new SimpleLogger(log);
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return logger;
        }
    }
}