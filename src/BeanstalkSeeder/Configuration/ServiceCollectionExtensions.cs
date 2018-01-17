using System;
using System.Collections.Generic;
using System.Net;
using Amazon;
using Amazon.Runtime;
using Amazon.SQS;
using BeanstalkSeeder.Options;
using BeanstalkSeeder.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BeanstalkSeeder.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IEnumerable<IDisposable> AddLogic(this IServiceCollection services,
            IConfigurationRoot configuration)
        {
            services.AddSingleton<MessagePump>();
            services.AddSingleton<QueueReader>();
            services.AddSingleton<WorkerInvoker>();

            var workerOptions = configuration.GetSection("Worker").Get<WorkerOptions>();

            if (!workerOptions.Endpoint.IsAbsoluteUri)
            {
                throw new ArgumentOutOfRangeException(
                    "Worker:Endpoint",
                    workerOptions.Endpoint.OriginalString,
                    "The Worker Endpoint is not a valid URI");
            }

            var sp = ServicePointManager.FindServicePoint(workerOptions.Endpoint);
            sp.ConnectionLeaseTimeout = 60 * 1000;

            var httpClient = new WorkerHttpClient
            {
                BaseAddress = workerOptions.Endpoint,
            };

            services.AddSingleton<IHttpClient>(httpClient);
            services.AddSingleton<IDelayer>(new Delayer());

            return new List<IDisposable>
            {
                httpClient
            };
        }

        public static void AddAws(this IServiceCollection services, IConfigurationRoot configuration)
        {
            var regionSystemName = configuration.GetValue<string>("Aws:RegionSystemName");

            var sqsClient = new AmazonSQSClient(new EnvironmentVariablesAWSCredentials(),
                RegionEndpoint.GetBySystemName(regionSystemName));

            services.AddSingleton<IAmazonSQS>(sqsClient);
        }

        public static void AddLogging(this IServiceCollection services, ILoggerFactory loggerFactory)
        {
            services.AddSingleton(loggerFactory);
            services.AddLogging();
        }

        public static void AddOptions(this IServiceCollection services, IConfigurationRoot configuration)
        {
            services.AddSingleton<IConfiguration>(configuration);
            services.AddOptions();
            services.Configure<QueueOptions>(configuration.GetSection("Aws:Queue"));
        }
    }
}