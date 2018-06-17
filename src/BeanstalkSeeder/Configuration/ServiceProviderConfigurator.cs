using System;
using System.Collections.Generic;
using BeanstalkSeeder.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeanstalkSeeder.Configuration
{
    public class ServiceProviderConfigurator : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public IServiceProvider ConfigureTheWorld(AwsOptions awsOptions, QueueOptions queueOptions, WorkerOptions workerOptions)
        {
            IServiceCollection services = new ServiceCollection();

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();

            if (IsDevelopment())
            {
                configurationBuilder.AddUserSecrets<Program>();
            }

            var configuration = configurationBuilder.Build();

            var loggerFactory = configuration.ConfigureSerilog();

            services.AddOptions(configuration, queueOptions);
            services.AddLogging(loggerFactory);
            services.AddAws(awsOptions, queueOptions);
            _disposables.AddRange(services.AddLogic(workerOptions));

            return services.BuildServiceProvider();
        }

        private static bool IsDevelopment()
        {
            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var isDevelopment = "Development".Equals(environment);
            return isDevelopment;
        }

        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception)
                {
                    // There is not much we can do at this stage
                }
            }
        }
    }
}