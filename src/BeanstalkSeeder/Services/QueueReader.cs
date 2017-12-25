using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using BeanstalkSeeder.Models;
using BeanstalkSeeder.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeanstalkSeeder.Services
{
    public class QueueReader
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly QueueOptions _queueOptions;
        private readonly ILogger<QueueReader> _logger;

        public Uri QueueUrl => _queueOptions.WorkerQueueUrl;

        public QueueReader(IAmazonSQS sqsClient, IOptions<QueueOptions> queueOptions, ILogger<QueueReader> logger)
        {
            _sqsClient = sqsClient;
            _queueOptions = queueOptions.Value;
            _logger = logger;
        }

        public async Task<WorkerMessage> ReadOneAsync(CancellationToken token)
        {
            _logger.LogInformation("Receiving message");
            
            var request = new ReceiveMessageRequest
            {
                QueueUrl = _queueOptions.WorkerQueueUrl.ToString(),
                MessageAttributeNames = new List<string> { "All" },
            };

            var response = await _sqsClient.ReceiveMessageAsync(request, token);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Could not receive message from SQS - response was {@ReceiveMessageResponse}", response);
                throw new InvalidOperationException("Could not receive message from SQS");
            }

            if (response.Messages.Count != 1) return null;
            
            var message = response.Messages.First();

            var workerMessage = new WorkerMessage
            {
                JsonPayload = message.Body,
                Headers = message.MessageAttributes.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.StringValue),
                ReceiptHandle = message.ReceiptHandle
            };
            
            _logger.LogInformation("Received {@Message}", message);

            return workerMessage;
        }

        public async Task DeleteAsync(string receiptHandle, CancellationToken token)
        {
            var response = await _sqsClient.DeleteMessageAsync(_queueOptions.WorkerQueueUrl.ToString(), receiptHandle, token);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Could not delete message from SQS - response was {@DeleteMessageResponse}", response);
                throw new InvalidOperationException("Could not delete message from SQS");
            }
            
            _logger.LogInformation("Deleted message {ReceiptHandle}", receiptHandle);
        }
    }
}