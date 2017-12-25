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

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            
            var loggerFactory = configuration.ConfigureSerilog();

            services.AddOptions(configuration);
            services.AddLogging(loggerFactory);
            services.AddAws(configuration);
            services.AddLogic();
            
            return services.BuildServiceProvider();
        }
    }
}