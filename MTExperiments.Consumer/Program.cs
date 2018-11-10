using System;
using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Messaging.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using MTUtils;
using IHost = Microsoft.Extensions.Hosting.IHost;

namespace MTExperiments.Consumer
{
    public class Program
    {
        public static TaskCompletionSource<int> TaskCompletionSource = new TaskCompletionSource<int>();
        static void Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(
                    (configurationBuilder => { configurationBuilder.AddEnvironmentVariables(); }))
                .ConfigureServices((hostingContext, serviceCollection) =>
                {
                    serviceCollection.AddTransient<ChangeCaseCommandHandler>();
                    serviceCollection.AddTransient<TerminateCommandHandler>();

                    serviceCollection.AddMassTransit(mt => { mt.AddConsumer<ChangeCaseCommandHandler>(); });

                    serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                    {
                        string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                        var host = cfg.Host(busConnectionString, hostConfiguration => { });
                        
                        cfg.CreateConventionalCommandHandlerEndpoint<ChangeCaseCommandHandler, ChangeCaseCommand>(provider);
                        cfg.CreateConventionalCommandHandlerEndpoint<TerminateCommandHandler, TerminateCommand>(provider);
                    }));
                });


            var runtime = builder.UseConsoleLifetime().Build();
            var bus = runtime.Services.GetService<IBusControl>();
            bus.Start();
            runtime.Start();
            Task.WaitAny(TaskCompletionSource.Task);
            bus.Stop();
            runtime.StopAsync(TimeSpan.FromSeconds(1)).Wait();
            //The app still doesn't stop even if I stop everything that I could. Some spawned processes are still hanging.
            //Environment.Exit(0);
            throw new Exception();
        }

    }

    public class TerminateCommandHandler : IConsumer<TerminateCommand>
    {
        private readonly IHostLifetime _hostLifetime;


        public TerminateCommandHandler(IHostLifetime hostLifetime)
        {
            _hostLifetime = hostLifetime;
        }
        public async Task Consume(ConsumeContext<TerminateCommand> context)
        {
            await _hostLifetime.StopAsync(CancellationToken.None);
            Program.TaskCompletionSource.SetResult(1);
        }
    }
}
