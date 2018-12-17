using System;
using System.Threading.Tasks;
using GreenPipes;
using GreenPipes.Agents;
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
        public const string TenantId = "Some_test_tenant_id";
        static async Task Main(string[] args)
        {
            //Uri hostUri = null;
            string address = null;
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
                        host.CreateConventionalCommandMapping<TerminateCommand>();
                        host.CreateConventionalCommandMapping<ScheduledCommand>();

                        address = host.Address.ToString();
                        cfg.ConfigurePublish(configurator =>
                        {
                            configurator.UseSendExecute(context =>
                            {
                                context.Headers.Set(MassTransitExtensions.TENANT_ID_KEY, TenantId);
                            });
                        });

                        cfg.UseServiceBusMessageScheduler();
                        cfg.ConfigureSend(configurator =>
                        {
                            configurator.UseSendExecute(context =>
                            {
                                context.Headers.Set(MassTransitExtensions.TENANT_ID_KEY, TenantId);
                            });
                        });
                        
                    }));

                    serviceCollection.AddSingleton<IPublishEndpoint>(provider => provider.GetService<IBusControl>());
                    serviceCollection.AddSingleton<ISendEndpointProvider>(provider => provider.GetService<IBusControl>());
                    
                });
            
            

            var config = builder.Build();
            var sendEndpointProvider = config.Services.GetService<ISendEndpointProvider>();
            var publishEndpoint = config.Services.GetService<IPublishEndpoint>();
            var busControl = config.Services.GetService<IBusControl>();
            
            await busControl.StartAsync();
            Console.WriteLine("Enter ';' if you want to finish");
            while (true)
            {
                var line = Console.ReadLine();
                if (line == ";")
                {
                    await sendEndpointProvider.Send<TerminateCommand>(new object());
                    return;
                }

                if (line.ToLower() == "a")
                {
                    await publishEndpoint.Publish<ObjectCreatedA>(new ObjectA()
                    {
                        A = "Value A",
                        SomeValue = "Some value from A"
                    });
                    continue;
                }

                if (line.ToLower() == "b")
                {
                    await publishEndpoint.Publish<ObjectCreatedB>(new ObjectB()
                    {
                        B = "Value B",
                        SomeValue = "Some value from B"
                    });
                    continue;
                }

                if (line.ToLower() == "s")
                {
                    var scheduledCommand = new ScheduledCommandImpl {ExecutedIn = 30, IsReallyScheduled = false};
                    await sendEndpointProvider.Send<ScheduledCommand>(scheduledCommand);
                    scheduledCommand.IsReallyScheduled = true;
                    var jobId = await publishEndpoint.ScheduleSend<ScheduledCommand>(new Uri(MassTransitExtensions.BuildConventionalAddress<ScheduledCommand>(address)), DateTime.UtcNow.AddSeconds(30),
                        scheduledCommand);
                    await publishEndpoint.ScheduleSend<ScheduledCommand>(new Uri(MassTransitExtensions.BuildConventionalAddress<ScheduledCommand>(address)), DateTime.UtcNow.AddHours(30),
                        scheduledCommand);
                    //await publishEndpoint.ScheduleSend<ExecuteActivity>(new Uri("ScheduledCommand", UriKind.Relative), DateTime.UtcNow.AddSeconds(45),
                    //    new ExecuteActivityImpl
                    //    {
                    //        ActivityId = "ABCD"
                    //    });


                    continue;
                }

                await sendEndpointProvider.Send<ChangeCaseCommand>(
                    new ChangeCaseCommandImpl
                    {
                        Text = line
                    });
            }
        }
    }
    

    class ObjectA : ObjectCreatedA
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SomeValue { get; set; }
        public string A { get; set; }
    }

    class ObjectB : ObjectCreatedB
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SomeValue { get; set; }
        public string B { get; set; }
    }
}
