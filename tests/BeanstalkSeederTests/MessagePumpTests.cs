using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;
using BeanstalkSeeder.Options;
using BeanstalkSeeder.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NSubstitute.ClearExtensions;
using Xunit;

namespace BeanstalkSeederTests
{
    public class MessagePumpTests
    {
        private readonly MessagePump _target;

        private const string ExpectedQueueUrl = "https://www.google.com.au/";

        private readonly IHttpClient _httpClient;
        private readonly IAmazonSQS _sqsClient;

        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly CancellationToken _expectedCancellationToken;

        private ReceiveMessageRequest _actualReceiveMessageRequest;
        private CancellationToken _actualReceiveMessageCancellationToken;
        private HttpRequestMessage _actualHttpRequest;
        private CancellationToken _actualHttpCancellationToken;
        private CancellationToken _actualDeleteMessageCancellationToken;
        private string _actualQueueUrlForDeleteMessage;
        private string _actualReceiptHandle;

        private readonly Message _expectedMessage = new Message
        {
            Body = "{'sob': 'cry'}",
            ReceiptHandle = Guid.NewGuid().ToString(),
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                {
                    "SomeKey", new MessageAttributeValue
                    {
                        DataType = "string",
                        StringValue = "Hi"
                    }
                }
            }
        };

        private int ReceiveCount
        {
            get => _receiveCount;
            set
            {
                _receiveCount = value;
                _onReceiveMessage();
            }
        }

        private int _receiveCount;
        private Action _onReceiveMessage;

        private readonly List<int> _actualDelays = new List<int>();

        public MessagePumpTests()
        {
            _httpClient = Substitute.For<IHttpClient>();

            var workerInvoker = new WorkerInvoker(_httpClient, new NullLogger<WorkerInvoker>());

            _sqsClient = Substitute.For<IAmazonSQS>();
            var queueOptions = new QueueOptions {WorkerQueueUrl = new Uri(ExpectedQueueUrl)};
            var warppedQueueOptions = new OptionsWrapper<QueueOptions>(queueOptions);

            var queueReader = new QueueReader(_sqsClient, warppedQueueOptions, new NullLogger<QueueReader>());

            var delayer = Substitute.For<IDelayer>();
            delayer
                .WhenForAnyArgs(d => d.DelayAsync(0, _expectedCancellationToken))
                .Do(ci => _actualDelays.Add(ci.Arg<int>()));

            _target = new MessagePump(workerInvoker, queueReader, delayer, new NullLogger<MessagePump>());

            _cancellationTokenSource = new CancellationTokenSource();
            _expectedCancellationToken = _cancellationTokenSource.Token;

            _onReceiveMessage = () =>
            {
                if (_receiveCount > 1)
                {
                    _cancellationTokenSource.Cancel();
                }
            };
        }

        [Fact]
        public async Task GivenCancelledToken_ThenNoReceiveFromQueue()
        {
            // Arrange

            _cancellationTokenSource.Cancel();

            // Act

            await _target.RunAsync(_expectedCancellationToken);

            // Assert

            await _sqsClient
                .DidNotReceiveWithAnyArgs()
                .ReceiveMessageAsync((ReceiveMessageRequest) null, _expectedCancellationToken);
        }

        [Fact]
        public async Task GivenMessageInQueue_WhenReceiveMessage_ThenExpectedReceiveRequest()
        {
            // Arrange

            SetupReceiveMessage(true);

            // Act

            await _target.RunAsync(_expectedCancellationToken);

            // Assert

            Assert.Equal(_expectedCancellationToken, _actualReceiveMessageCancellationToken);
            Assert.Equal(ExpectedQueueUrl, _actualReceiveMessageRequest.QueueUrl);
            Assert.Equal(new List<string> {"All"}, _actualReceiveMessageRequest.MessageAttributeNames);
        }

        [Fact]
        public async Task GivenReceivedMessageFromQueue_WhenCallWorker_ThenExpectedHttpRequest()
        {
            // Arrange

            SetupReceiveMessage();
            SetupHttpClient(true);

            // Act

            await _target.RunAsync(_expectedCancellationToken);

            // Assert

            Assert.Equal(_expectedCancellationToken, _actualHttpCancellationToken);

            Assert.Equal(HttpMethod.Post, _actualHttpRequest.Method);
            Assert.Equal("application/json", _actualHttpRequest.Content.Headers.ContentType.MediaType);
            Assert.Equal("utf-8", _actualHttpRequest.Content.Headers.ContentType.CharSet);

            var actualHeaders = _actualHttpRequest.Headers;
            foreach (var messageAttribute in _expectedMessage.MessageAttributes)
            {
                var expectedHeaderName = $"X-Aws-Sqsd-Attr-{messageAttribute.Key}";

                var gotHeader = actualHeaders.TryGetValues(expectedHeaderName, out var headerValues);

                Assert.True(gotHeader, $"Header '{expectedHeaderName}' is missing");
                Assert.Equal(new List<string> {messageAttribute.Value.StringValue}, headerValues);
            }

            var actualBody = await _actualHttpRequest.Content.ReadAsStringAsync();
            Assert.Equal(_expectedMessage.Body, actualBody);
        }

        [Fact]
        public async Task GivenCalledWorker_WhenDeleteMessage_ThenExpectedDeleteRequest()
        {
            // Arrange

            SetupReceiveMessage();
            SetupHttpClient();
            SetupDeleteMessage(true);

            // Act

            await _target.RunAsync(_expectedCancellationToken);

            // Assert

            Assert.Equal(_expectedCancellationToken, _actualDeleteMessageCancellationToken);
            Assert.Equal(ExpectedQueueUrl, _actualQueueUrlForDeleteMessage);
            Assert.Equal(_expectedMessage.ReceiptHandle, _actualReceiptHandle);
        }

        [Fact]
        public async Task GivenNoMessageInQueue_ThenReachMaxBackoff_AndThenStopIncreasingBackoff()
        {
            // Arrange

            SetupReceiveNoMessage();

            _onReceiveMessage = () =>
            {
                if (_receiveCount > 15)
                {
                    _cancellationTokenSource.Cancel();
                }
            };

            // Act

            await _target.RunAsync(_expectedCancellationToken);

            // Assert

            var expectedDelays = new List<int>
            {
                500,
                600,
                720,
                864,
                1037,
                1245,
                1494,
                1793,
                2152,
                2583,
                3100,
                3720,
                4464,
                5357,
                5357,
                5357
            };

            Assert.Equal(expectedDelays, _actualDelays);
        }

        [Fact]
        public async Task
            GivenTwoReceiveWithoutMessage_AndGivenOneReceiveWithMessage_AndGivenTwoReceiveWithoutMessage_ThenResetMaxBackoff_AndThenStartIncreasingBackoff()
        {
            // Arrange

            SetupReceiveNoMessage();

            _onReceiveMessage = () =>
            {
                if (_receiveCount == 2) SetupReceiveMessage();
                if (_receiveCount == 3) SetupReceiveNoMessage();

                if (_receiveCount > 4)
                {
                    _cancellationTokenSource.Cancel();
                }
            };

            // Act

            await _target.RunAsync(_expectedCancellationToken);

            // Assert

            var expectedDelays = new List<int>
            {
                500,
                600,
                500,
                600
            };

            Assert.Equal(expectedDelays, _actualDelays);
        }

        private void SetupHttpClient(bool cancelToken = false)
        {
            _httpClient
                .SendAsync(null, _expectedCancellationToken)
                .ReturnsForAnyArgs(ci =>
                    {
                        _actualHttpRequest = ci.Arg<HttpRequestMessage>();
                        _actualHttpCancellationToken = ci.Arg<CancellationToken>();

                        if (cancelToken) _cancellationTokenSource.Cancel();

                        return new HttpResponseMessage(HttpStatusCode.OK);
                    }
                );
        }

        private void SetupReceiveNoMessage(bool cancelToken = false)
        {
            SetupReceiveMessageInner(false, cancelToken);
        }

        private void SetupReceiveMessage(bool cancelToken = false)
        {
            SetupReceiveMessageInner(true, cancelToken);
        }

        private void SetupReceiveMessageInner(bool hasMessage, bool cancelToken)
        {
            _sqsClient.ClearSubstitute();

            _sqsClient
                .ReceiveMessageAsync((ReceiveMessageRequest) null, _expectedCancellationToken)
                .ReturnsForAnyArgs(ci =>
                {
                    ReceiveCount++;

                    _actualReceiveMessageRequest = ci.Arg<ReceiveMessageRequest>();
                    _actualReceiveMessageCancellationToken = ci.Arg<CancellationToken>();

                    if (cancelToken) _cancellationTokenSource.Cancel();

                    return new ReceiveMessageResponse
                    {
                        HttpStatusCode = HttpStatusCode.OK,
                        Messages = hasMessage ? new List<Message> {_expectedMessage} : new List<Message>()
                    };
                });
        }


        private void SetupDeleteMessage(bool cancelToken = false)
        {
            _sqsClient
                .DeleteMessageAsync(null, null, _expectedCancellationToken)
                .ReturnsForAnyArgs(ci =>
                {
                    _actualQueueUrlForDeleteMessage = ci.ArgAt<string>(0);
                    _actualReceiptHandle = ci.ArgAt<string>(1);
                    _actualDeleteMessageCancellationToken = ci.Arg<CancellationToken>();

                    if (cancelToken) _cancellationTokenSource.Cancel();

                    return new DeleteMessageResponse
                    {
                        HttpStatusCode = HttpStatusCode.OK
                    };
                });
        }
    }
}