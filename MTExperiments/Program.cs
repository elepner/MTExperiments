using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.Azure.ServiceBus.Core.Configurators;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Messaging.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MTUtils;

namespace MTExperiments
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = new HostBuilder()
                .ConfigureAppConfiguration(
                    (configurationBuilder => { configurationBuilder.AddEnvironmentVariables(); }))
                .ConfigureServices((hostingContext, serviceCollection) =>
                {
                    serviceCollection.AddMassTransit();
                    serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                    {
                        string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                        var host = cfg.Host(busConnectionString, hostConfiguration => { });
                        host.CreateConventionalCommandMapping<ChangeCaseCommand>();
                    }));

                    serviceCollection.AddSingleton<IPublishEndpoint>(provider => provider.GetService<IBusControl>());
                    serviceCollection.AddSingleton<ISendEndpointProvider>(provider => provider.GetService<IBusControl>());
                });
            
            //var busControl = Bus.Factory.CreateUsingAzureServiceBus(cfg =>
            //{
            //    var host = cfg.Host(builder.)
            //});
            var config = builder.Build();
            var sendEndpointProvider = config.Services.GetService<ISendEndpointProvider>();
            var busControl = config.Services.GetService<IBusControl>();
            await busControl.StartAsync();
            Console.WriteLine("Enter ';' if you want to finish");
            while (true)
            {
                var line = Console.ReadLine();
                if (line == ";") return;

                await sendEndpointProvider.Send<ChangeCaseCommand>(new ChangeCaseCommandImpl
                {
                    Text = line
                });
            }
        }
    }
}
