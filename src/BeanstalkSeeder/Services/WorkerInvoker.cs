using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeanstalkSeeder.Models;
using BeanstalkSeeder.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BeanstalkSeeder.Services
{
    public class WorkerInvoker : IDisposable
    {
        private readonly WorkerOptions _options;
        private readonly ILogger<WorkerInvoker> _logger;

        private readonly HttpClient _httpClient;

        public Uri Endpoint => _options.Endpoint;

        public WorkerInvoker(IOptions<WorkerOptions> options, ILogger<WorkerInvoker> logger)
        {
            _options = options.Value;
            _logger = logger;

            _httpClient = new HttpClient
            {
                BaseAddress = _options.Endpoint,
            };
        }

        public async Task InvokeAsync(WorkerMessage message, CancellationToken token)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            _logger.LogInformation("Calling worker");

            var request = new HttpRequestMessage
            {
                Content = new StringContent(message.JsonPayload, Encoding.UTF8, "application/json"),
                Method = HttpMethod.Post,
            };

            message.Headers.Keys.ToList()
                .ForEach(k => request.Headers.Add($"X-Aws-Sqsd-Attr-{k}", message.Headers[k]));

            _logger.LogInformation("Calling worker with payload {JsonPayload} and headers {BeanstalkHeaders}",
                message.JsonPayload,
                request.Headers
                    .Where(h => h.Key.StartsWith("X-Aws-Sqsd-Attr-"))
                    .ToDictionary(kvp => kvp.Key, kvp => string.Join(",", kvp.Value)));

            var response = await _httpClient.SendAsync(request, token);

            response.EnsureSuccessStatusCode();
            
            _logger.LogInformation("Worker call was successful");
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}