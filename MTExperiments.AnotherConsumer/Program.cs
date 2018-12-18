using System;
using System.Threading.Tasks;
using GreenPipes;
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
        public static Random Random = new Random();
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
                        //host.CreateConventionalCommandMapping<ScheduledCommand>();
                        cfg.UseServiceBusMessageScheduler();
                        ConfigureBusEndpoints(cfg, provider, host);
                        host.CreateConventionalCommandMapping<ChangeCaseCommand>();

                    }));
                });

            

            var runtime = builder.UseConsoleLifetime().Build();
            var bus = runtime.Services.GetService<IBusControl>();
            bus.Start();
            await runtime.StartAsync();
        }

        static void ConfigureBusEndpoints(IServiceBusBusFactoryConfigurator cfg, IServiceProvider provider, IServiceBusHost host)
        {
            const string subsriberName = "AnotherSubscriber";
            cfg.SubscriptionEndpoint<AnotherThingHappened>(host, subsriberName, configurator =>
            {
                configurator.Handler<AnotherThingHappened>(context =>
                {
                    Console.WriteLine(context.Message.AnotherThingType);
                    if (Random.NextDouble() < 0.1)
                    {
                        throw new Exception("Oups, I failed :(");
                    }
                    return Task.CompletedTask;
                });

                
            });
            
            cfg.SubscriptionEndpoint<ObjectCreatedA>(host, subsriberName, configurator =>
            {
                configurator.Consumer<ObjectACreatedEventHandler>();
            });
            
            cfg.ReceiveEndpoint(host, queueName: "AnotherSubscirber2", configure: configurator =>
            {
                configurator.Handler<ObjectCreatedB>(async context =>
                {
                    Console.WriteLine("Another subscirber, object b created");
                    await context.SchedulePublish<ScheduledCommand>(TimeSpan.FromSeconds(30), new ScheduledCommandImpl
                    {
                        ExecutedIn = 30, IsReallyScheduled = true
                    });
                    
                    ChangeCaseCommand changeCase = new ChangeCaseCommandImpl
                    {
                        IsScheduled = true,
                        Text = "Hello world"
                    };

                    await context.ScheduleSendConventional(TimeSpan.FromSeconds(15), changeCase);
                });
            });

            cfg.CreateConventionalCommandHandlerEndpoint<DoAnotherThingCommandHandler, DoAnotherThingCommand>(provider);

        }

    }

    internal class ObjectBCreatedEventHandler : IConsumer<ObjectCreatedB>
    {
        public Task Consume(ConsumeContext<ObjectCreatedB> context)
        {
            Console.WriteLine("Object B Created. Another subscriber, Subscription endpoint");
            return Task.CompletedTask;
        }
    }

    internal class ObjectACreatedEventHandler : IConsumer<ObjectCreatedA>
    {
        public Task Consume(ConsumeContext<ObjectCreatedA> context)
        {
            if (Program.Random.NextDouble() < 0.1)
            {
                throw new Exception("Oups, I failed in object A consumer :(");
            }
            Console.WriteLine("Object A Created");
            return Task.CompletedTask;
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

    class ExecuteActivityCommandHandler : IConsumer<ExecuteActivity>
    {
        public Task Consume(ConsumeContext<ExecuteActivity> context)
        {
            Console.WriteLine("Doing activity: {0}", context.Message.ActivityId);
            return Task.CompletedTask;
        }
    }
}
