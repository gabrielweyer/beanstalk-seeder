using System;
using System.Threading;
using BeanstalkSeeder.Configuration;
using BeanstalkSeeder.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BeanstalkSeeder
{
    class Program
    {
        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private static readonly CancellationToken Token = Cts.Token;

        static void Main(string[] args)
        {
            Console.Clear();
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            try
            {
                var provider = ServiceProviderConfigurator.ConfigureTheWorld();
                
                using (var applicationScope = provider.CreateScope())
                {
                    var messagePump = applicationScope
                        .ServiceProvider
                        .GetRequiredService<MessagePump>();

                    messagePump.RunAsync(Token).Wait();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0} - Message: {1}", e.GetType(), e.Message);
            }
            finally
            {
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs consoleCancelEventArgs)
        {
            Console.WriteLine("ConsoleCancelEvent received => Cancelling token");
            consoleCancelEventArgs.Cancel = true;
            Cts.Cancel();
        }
    }
}