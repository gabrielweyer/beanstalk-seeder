using System;
using System.Diagnostics;
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
                using (var providerConfigurator = new ServiceProviderConfigurator())
                using (var applicationScope = providerConfigurator.ConfigureTheWorld().CreateScope())
                {
                    var messagePump = applicationScope
                        .ServiceProvider
                        .GetRequiredService<MessagePump>();

                    messagePump.RunAsync(Token).Wait();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.GetType());
                Console.WriteLine("Message: {0}", e.Message);
                Console.WriteLine("StackTrace:");
                Console.WriteLine(e.Demystify().StackTrace);
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