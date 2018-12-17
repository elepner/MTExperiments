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
                    serviceCollection.AddTransient<GenericConsumer>();
                    serviceCollection.AddMassTransit(mt => { mt.AddConsumer<ChangeCaseCommandHandler>(); });
                    serviceCollection.AddTransient<ScheduledMessageConsumer>();
                    serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg=>
                    {
                        string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                        var host = cfg.Host(busConnectionString, hostConfiguration => { });
                        
                        cfg.CreateConventionalCommandHandlerEndpoint<ChangeCaseCommandHandler, ChangeCaseCommand>(provider);
                        cfg.CreateConventionalCommandHandlerEndpoint<TerminateCommandHandler, TerminateCommand>(provider);
                        //cfg.CreateConventionalCommandHandlerEndpoint<ScheduledMessageConsumer, ScheduledCommand>(provider);
                        host.CreateConventionalCommandMapping<DoAnotherThingCommand>();
                        cfg.ConfigureExtraHeadersCopying();
                        cfg.UseServiceBusMessageScheduler();
                        string topicTpl = "Messaging.Contracts/ObjectCreated";

                        
                        cfg.SubscriptionEndpoint<ObjectCreatedA>(host, "ConsumerApp", configurator =>
                        {
                            configurator.Consumer<GenericConsumer>();
                        });

                        cfg.SubscriptionEndpoint<ObjectCreatedB>(host, "ConsumerApp", configurator =>
                        {
                            configurator.Consumer<GenericConsumer>();
                        });

                        //cfg.SubscriptionEndpoint<ScheduledCommand>(host, "ScheduledCommand", configurator =>
                        //{
                        //    configurator.Consumer<ScheduledMessageConsumer>();
                        //});

                        cfg.SubscriptionEndpoint<ScheduledCommand>(host, "ConsumerApp", configurator =>
                        {
                            configurator.Consumer<ScheduledMessageConsumer>();
                        });
                        //cfg.ReceiveEndpoint("ScheduledCommand", configurator =>
                        //{
                        //    configurator.Consumer<ScheduledMessageConsumer>();
                        //});
                    }));
                });


            var runtime = builder.UseConsoleLifetime().Build();
            var bus = runtime.Services.GetService<IBusControl>();
            bus.Start();
            TaskCompletionSource.Task.Wait();
            bus.Stop();
        }

    }

    public class GenericConsumer : IConsumer<ObjectCreated>
    {
        private static int Count = 0;
        public async Task Consume(ConsumeContext<ObjectCreated> context)
        {
            Console.WriteLine("Recieved object:");
            Console.WriteLine(context.Message.Id);
            
            await context.Send<DoAnotherThingCommand>(new
            {
                ThingType = context.Message.SomeValue
            });
        }
    }

    public class ScheduledMessageConsumer : IConsumer<ScheduledCommand>
    {
        public Task Consume(ConsumeContext<ScheduledCommand> context)
        {
            Console.WriteLine("Recieved Scheduled command: {0}, is Really scheduled: {1}", context.Message.ExecutedIn, context.Message.IsReallyScheduled);
            return Task.CompletedTask;
        }
    }

    public class ConsumerA : IConsumer<ObjectCreatedA>
    {
        public async Task Consume(ConsumeContext<ObjectCreatedA> context)
        {
            Console.WriteLine("Recieved object A:");
            Console.WriteLine(context.Message.Id);

            await context.ScheduleSend<ScheduledCommand>(TimeSpan.FromSeconds(30), new ScheduledCommandImpl
            {
                ExecutedIn = 30,
                IsReallyScheduled = true
            });

            await context.Send<DoAnotherThingCommand>(new
            {
                ThingType = context.Message.SomeValue
            });
        }
        
    }
}
