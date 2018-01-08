using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace BeanstalkSeeder.Services
{
    internal class WorkerHttpClient : HttpClient, IHttpClient
    {
    }

    public interface IHttpClient
    {
        Uri BaseAddress { get; }
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token);
    }
}