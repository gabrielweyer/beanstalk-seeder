using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Microsoft.Extensions.Logging;

namespace BeanstalkSeeder.Services
{
    public class MessagePump
    {
        private readonly WorkerInvoker _workerInvoker;
        private readonly QueueReader _queueReader;
        private readonly IDelayer _delayer;
        private readonly ILogger<MessagePump> _logger;

        private const int DefaultBackoffMilliseconds = 500;

        public MessagePump(WorkerInvoker workerInvoker, QueueReader queueReader, IDelayer delayer, ILogger<MessagePump> logger)
        {
            _workerInvoker = workerInvoker;
            _queueReader = queueReader;
            _delayer = delayer;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken token)
        {
            _logger.LogInformation("Starting message pump");
            _logger.LogDebug("Reading from queue {QueueUrl}", _queueReader.QueueUrl);
            _logger.LogDebug("Using Worker {WorkerEndpoint}", _workerInvoker.Endpoint);

            var backoffMillisecond = DefaultBackoffMilliseconds;

            while (true)
            {
                try
                {
                    if (token.IsCancellationRequested)
                    {
                        _logger.LogInformation("Exiting message pump because cancellation was requested");
                        break;
                    }

                    var message = await _queueReader.ReadOneAsync(token);

                    if (message == null)
                    {
                        _logger.LogInformation(
                            "There is no available message in the queue, sleeping for {BackoffMilliseconds} ms",
                            backoffMillisecond);

                        await _delayer.DelayAsync(backoffMillisecond, token);

                        if (backoffMillisecond < 5000)
                        {
                            backoffMillisecond = (int) Math.Ceiling(backoffMillisecond * 1.2);
                        }

                        continue;
                    }

                    backoffMillisecond = DefaultBackoffMilliseconds;

                    await _workerInvoker.InvokeAsync(message, token);

                    await _queueReader.DeleteAsync(message.ReceiptHandle, token);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogDebug(
                        "A Task was cancelled because cancellation was requested, if this was because the process was exited this error can be ignored safely");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(new EventId(1), ex, "Error when calling the worker");
                }
                catch (AmazonSQSException ex) when ("AWS.SimpleQueueService.NonExistentQueue".Equals(ex.ErrorCode))
                {
                    _logger.LogError("The queue {QueueUrl} does not exist", _queueReader.QueueUrl);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(1), ex, "Error when receiving / processing a message");
                }
            }
        }
    }
}