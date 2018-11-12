﻿using System;
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

                    serviceCollection.AddSingleton(provider => Bus.Factory.CreateUsingAzureServiceBus(cfg =>
                    {
                        string busConnectionString = hostingContext.Configuration["MY_TEST_ASB"];

                        var host = cfg.Host(busConnectionString, hostConfiguration => { });
                        
                        cfg.CreateConventionalCommandHandlerEndpoint<ChangeCaseCommandHandler, ChangeCaseCommand>(provider);
                        cfg.CreateConventionalCommandHandlerEndpoint<TerminateCommandHandler, TerminateCommand>(provider);
                        
                        string topicTpl = "Messaging.Contracts/ObjectCreated";
                        

                        foreach(string topic in new[] { "A", "B" })
                        {
                            cfg.SubscriptionEndpoint(host, "ConsumerApp", topicTpl + topic, configurator =>
                            {
                                configurator.Consumer<GenericConsumer>();
                            });
                        }
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
        public Task Consume(ConsumeContext<ObjectCreated> context)
        {
            Console.WriteLine("Recieved object:");
            Console.WriteLine(context.Message.Id);
            return Task.CompletedTask;
        }
    }
}
