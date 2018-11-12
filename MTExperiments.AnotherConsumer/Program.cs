using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using Messaging.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MTExperiments.AnotherConsumer
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
                    
                    serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                    {
                        string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                        var host = cfg.Host(busConnectionString, hostConfiguration => { });
                        
                        cfg.SubscriptionEndpoint<AnotherThingHappened>(host, "AnotherSubscriber", configurator =>
                        {
                            configurator.Handler<AnotherThingHappened>(context =>
                            {
                                System.Console.WriteLine(context.Message.AnotherThingType);
                                return Task.CompletedTask;
                            });
                        });

                        
                    }));
                });


            var runtime = builder.UseConsoleLifetime().Build();
            var bus = runtime.Services.GetService<IBusControl>();
            bus.Start();
            await runtime.StartAsync();
        }
    }
}
