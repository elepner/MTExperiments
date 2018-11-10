using System;
using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Messaging.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MTUtils;

namespace MTExperiments.Consumer
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
                    serviceCollection.AddTransient<ChangeCaseHandler>();
                    serviceCollection.AddMassTransit(mt => { mt.AddConsumer<ChangeCaseHandler>(); });

                    serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                    {
                        string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                        var host = cfg.Host(busConnectionString, hostConfiguration => { });

                        cfg.CreateConventionalCommandEndpoint<ChangeCaseCommand>(host,
                            (configurator =>
                            {
                                configurator.Consumer<ChangeCaseHandler>(provider);
                                
                            }));


                    }));
                });


            var runtime = builder.Build();
            await runtime.Services.GetService<IBusControl>().StartAsync();
            var handler = runtime.Services.GetService<ChangeCaseHandler>();
            await runtime.StartAsync();
            //await builder.RunConsoleAsync();
        }

    }

    public class ChangeCaseHandler : IConsumer<ChangeCaseCommand>
    {
        public Task Consume(ConsumeContext<ChangeCaseCommand> context)
        {
            string message = context.Message.Text;
            foreach (char c in message)
            {
                Char toPrint;
                if (Char.IsUpper(c))
                {
                    toPrint = Char.ToLower(c);
                }
                else if (Char.IsLower(c))
                {
                    toPrint = Char.ToUpper(c);
                }
                else
                {
                    toPrint = c;
                }

                Console.Write(toPrint);
            }
            Console.WriteLine();
            return Task.CompletedTask;
        }
    }
}
