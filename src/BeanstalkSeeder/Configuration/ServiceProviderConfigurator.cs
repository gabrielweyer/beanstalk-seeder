using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeanstalkSeeder.Configuration
{
    public static class ServiceProviderConfigurator
    {
        public static IServiceProvider ConfigureTheWorld()
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

            services.AddOptions(configuration);
            services.AddLogging(loggerFactory);
            services.AddAws(configuration);
            services.AddLogic();
            
            return services.BuildServiceProvider();
        }

        private static bool IsDevelopment()
        {
            var environment = Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT");
            var isDevelopment = "Development".Equals(environment);
            return isDevelopment;
        }
    }
}