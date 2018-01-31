﻿using System;
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
            var regionSystemName = configuration.GetValue<string>("Aws:Queue:WorkerQueueUrl");

            var sqsClient = new AmazonSQSClient(new EnvironmentVariablesAWSCredentials(),
                GetRegionEndpoint(regionSystemName));

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

        /// <summary>
        /// Based on https://docs.aws.amazon.com/AWSEC2/latest/UserGuide/using-regions-availability-zones.html#concepts-available-regions
        /// </summary>
        /// <param name="queueUrl"></param>
        /// <returns></returns>
        public static RegionEndpoint GetRegionEndpoint(string queueUrl)
        {
            var firstDotIndex = 0;
            var secondDotIndex = 0;
            var foundFirstDotIndex = false;

            for (var i = 0; i < queueUrl.Length; i++)
            {
                if (queueUrl[i] != '.') continue;

                if (foundFirstDotIndex)
                {
                    secondDotIndex = i;
                    break;
                }

                firstDotIndex = i;
                foundFirstDotIndex = true;
            }

            if (secondDotIndex == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(queueUrl),
                    queueUrl,
                    "Should contain the region system name as a sub-domain.");
            }

            var regionSystemName = queueUrl.Substring(++firstDotIndex, secondDotIndex - firstDotIndex);
            return RegionEndpoint.GetBySystemName(regionSystemName);
        }
    }
}