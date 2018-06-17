using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BeanstalkSeeder.Configuration;
using BeanstalkSeeder.Options;
using BeanstalkSeeder.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;

namespace BeanstalkSeeder
{
    class Program
    {
        [Uri]
        [Required]
        [Option(ShortName = "w", LongName = "worker", Description = "Accessible URI of the Beanstalk Worker")]
        public Uri WorkerUri { get; }

        [Uri]
        [Required]
        [Option(ShortName = "q", LongName = "queue", Description = "URI of the SQS queue")]
        public Uri WorkerQueueUri { get; }

        private static readonly CancellationTokenSource Cts = new CancellationTokenSource();
        private static readonly CancellationToken Token = Cts.Token;

        static Task<int> Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        private async Task OnExecuteAsync()
        {
            var awsOptions = new AwsOptions
            {
                AccessKey = Prompt.GetPassword("AWS Access Key", ConsoleColor.White, ConsoleColor.DarkBlue),
                SecretKey = Prompt.GetPassword("AWS Secret Key", ConsoleColor.White, ConsoleColor.DarkBlue)
            };

            var queueOptions = new QueueOptions {WorkerQueueUrl = WorkerQueueUri};
            var workerOptions = new WorkerOptions {Endpoint = WorkerUri};

            Console.Clear();
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            try
            {
                using (var providerConfigurator = new ServiceProviderConfigurator())
                using (var applicationScope = providerConfigurator
                    .ConfigureTheWorld(awsOptions, queueOptions, workerOptions).CreateScope())
                {
                    var messagePump = applicationScope
                        .ServiceProvider
                        .GetRequiredService<MessagePump>();

                    await messagePump.RunAsync(Token);
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