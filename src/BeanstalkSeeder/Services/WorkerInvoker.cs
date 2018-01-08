using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeanstalkSeeder.Models;
using Microsoft.Extensions.Logging;

namespace BeanstalkSeeder.Services
{
    public class WorkerInvoker
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger<WorkerInvoker> _logger;

        public Uri Endpoint => _httpClient.BaseAddress;

        public WorkerInvoker(IHttpClient httpClient, ILogger<WorkerInvoker> logger)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public async Task InvokeAsync(WorkerMessage message, CancellationToken token)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

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
    }
}