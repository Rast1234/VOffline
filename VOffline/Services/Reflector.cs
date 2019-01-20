using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using VOffline.Services.Handlers;

namespace VOffline.Services
{
    public class Reflector
    {
        private readonly IServiceProvider serviceProvider;

        public Reflector(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<IEnumerable<object>> ProcessJob(object job, long i, CancellationToken token, ILog log)
        {
            var jobType = GetJobType(job);
            var handlerGenericType = typeof(IHandler<>);
            Type[] typeArgs = { jobType };
            var handlerType = handlerGenericType.MakeGenericType(typeArgs);
            var methodInfo = handlerType.GetMethod(nameof(IHandler<object>.Process));

            using (var scope = serviceProvider.CreateScope())
            {
                var handlerObj = scope.ServiceProvider.GetRequiredService(handlerType);
                var invokeResult = methodInfo.Invoke(handlerObj, new[] { job, token, log }) as Task<IEnumerable<object>>;
                return await invokeResult;
            }
            
        }

        private static Type GetJobType(object job)
        {
            var jobType = job.GetType();

            var interfaces = jobType.GetInterfaces();
            if (interfaces.Length == 0)
            {
                return jobType;
            }

            if (interfaces.Length == 1)
            {
                return interfaces[0];
            }

            throw new InvalidOperationException($"Job [{jobType.FullName}] implements multiple interfaces, expected 0 or 1");
        }
    }
}