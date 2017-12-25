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
        public static void AddLogic(this IServiceCollection services)
        {
            services.AddSingleton<MessagePump>();
            services.AddSingleton<QueueReader>();
            services.AddSingleton<WorkerInvoker>();
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
            services.Configure<WorkerOptions>(configuration.GetSection("Worker"));
            services.Configure<QueueOptions>(configuration.GetSection("Aws:Queue"));
        }
    }
}