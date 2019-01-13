using System;
using log4net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Log4Net.AspNetCore.Extensions;

namespace VOffline.Services.VkNetHacks
{
    public class SimpleLogger : ILogger
    {
        private readonly ILog log;

        public SimpleLogger(ILog log)
        {
            this.log = log;
        }

        public string Name => log.Logger.Name;

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                case LogLevel.Debug:
                    return this.log.IsDebugEnabled;
                case LogLevel.Information:
                    return this.log.IsInfoEnabled;
                case LogLevel.Warning:
                    return this.log.IsWarnEnabled;
                case LogLevel.Error:
                    return this.log.IsErrorEnabled;
                case LogLevel.Critical:
                    return this.log.IsFatalEnabled;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        public void Log<TState>(LogLevel logLevel,EventId eventId,TState state,Exception exception,Func<TState, Exception, string> formatter)
        {
            if (!this.IsEnabled(logLevel))
                return;
            if (formatter == null)
                throw new ArgumentNullException(nameof(formatter));
            string str = formatter(state, exception);
            if (string.IsNullOrEmpty(str) && exception == null)
                return;
            switch (logLevel)
            {
                case LogLevel.Trace:
                    this.log.Trace((object)str, exception);
                    break;
                case LogLevel.Debug:
                    this.log.Debug((object)str, exception);
                    break;
                case LogLevel.Information:
                    this.log.Info((object)str, exception);
                    break;
                case LogLevel.Warning:
                    this.log.Warn((object)str, exception);
                    break;
                case LogLevel.Error:
                    this.log.Error((object)str, exception);
                    break;
                case LogLevel.Critical:
                    this.log.Fatal((object)str, exception);
                    break;
                default:
                    this.log.Warn((object)string.Format("Encountered unknown log level {0}, writing out as Info.", (object)logLevel));
                    this.log.Info((object)str, exception);
                    break;
            }
        }
    }
}