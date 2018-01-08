using System.Threading;
using System.Threading.Tasks;

namespace BeanstalkSeeder.Services
{
    internal class Delayer : IDelayer
    {
        public async Task DelayAsync(int millisecondsDelay, CancellationToken token)
        {
            await Task.Delay(millisecondsDelay, token);
        }
    }

    public interface IDelayer
    {
        Task DelayAsync(int millisecondsDelay, CancellationToken token);
    }
}