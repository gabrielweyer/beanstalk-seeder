using System;
using Amazon.SQS.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;

namespace BeanstalkSeeder.Configuration
{
    public static class LoggerConfigurator
    {
        public static ILoggerFactory ConfigureSerilog(this IConfigurationRoot configuration)
        {
            var serilogLevel = configuration.GetLoggingLevel("Serilog");

            var loggerConfiguration = new LoggerConfiguration()
                .Destructure.ByTransforming<MessageAttributeValue>(Destructure)
                .Destructure.ByTransforming<Message>(Destructure)
                .MinimumLevel.Is(serilogLevel)
                .Enrich.WithDemystifiedStackTraces()
                .Enrich.FromLogContext()
                .WriteTo.Console(serilogLevel);

            var logger = loggerConfiguration.CreateLogger();

            ILoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddSerilog(logger);

            return loggerFactory;
        }

        /// <summary>
        /// See https://docs.aws.amazon.com/AWSSimpleQueueService/latest/SQSDeveloperGuide/sqs-message-attributes.html#message-attributes-data-types-validation
        /// </summary>
        /// <param name="attribute"></param>
        /// <returns></returns>
        private static object Destructure(MessageAttributeValue attribute)
        {
            if (attribute.DataType.StartsWith("String", StringComparison.InvariantCultureIgnoreCase))
            {
                return new {attribute.DataType, attribute.StringValue};
            }

            if (attribute.DataType.StartsWith("Number", StringComparison.InvariantCultureIgnoreCase))
            {
                return new {attribute.DataType, attribute.StringValue};
            }

            if (attribute.DataType.StartsWith("Binary", StringComparison.InvariantCultureIgnoreCase))
            {
                return new {attribute.DataType, BinaryValue = Convert.ToBase64String(attribute.BinaryValue.ToArray())};
            }

            throw new ArgumentOutOfRangeException(
                nameof(attribute.DataType),
                attribute.DataType,
                "This DataType is not supported.");
        }

        private static object Destructure(Message message)
        {
            return new
            {
                message.MessageAttributes,
                message.Body,
                message.Attributes,
                message.MD5OfBody,
                message.MD5OfMessageAttributes,
                message.MessageId,
                message.ReceiptHandle
            };
        }
    }
}