using System;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MTExperiments.Consumer;

namespace MTExperiments.EventHandler
{
    class Program
    {
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
                        
                    }));
                });


            var runtime = builder.UseConsoleLifetime().Build();
            var bus = runtime.Services.GetService<IBusControl>();
            bus.Start();
            runtime.Start();
        }
    }
}
