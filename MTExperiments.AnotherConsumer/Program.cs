using System.Threading.Tasks;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;
using Messaging.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MTUtils;

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
                    serviceCollection.AddTransient<DoAnotherThingCommandHandler>();
                    serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                    {
                        string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                        var host = cfg.Host(busConnectionString, hostConfiguration => { });

                        cfg.SubscriptionEndpoint<AnotherThingHappened>(host, "AnotherSubscriber", configurator =>
                        {
                            configurator.Handler<AnotherThingHappened>(context =>
                            {
                                context.Headers.TryGetHeader(MassTransitExtensions.TENANT_ID_KEY, out var tenantId);
                                System.Console.Write(tenantId);
                                System.Console.WriteLine(context.Message.AnotherThingType);
                                return Task.CompletedTask;
                            });
                        });

                        cfg.CreateConventionalCommandHandlerEndpoint<DoAnotherThingCommandHandler, DoAnotherThingCommand>(provider);
                        
                    }));
                });


            var runtime = builder.UseConsoleLifetime().Build();
            var bus = runtime.Services.GetService<IBusControl>();
            bus.Start();
            await runtime.StartAsync();
        }
    }

    internal class DoAnotherThingCommandHandler : IConsumer<DoAnotherThingCommand>
    {
        public Task Consume(ConsumeContext<DoAnotherThingCommand> context)
        {
            System.Console.WriteLine($"I'm doing another thing because I received command: {context.Message.ThingType}. In Tenant from header: {context.Headers.Get<string>(MassTransitExtensions.TENANT_ID_KEY)}");
            return Task.CompletedTask;
        }
    }
}
