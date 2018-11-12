using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GreenPipes;
using MassTransit;


namespace MTUtils
{
    public static class MassTransitExtensions
    {
        public static string BuildConventionalAddress<TMessage>(string hostName)
        {
            return $"{hostName}{typeof(TMessage).FullName.ToLower().Replace(".", "_")}";
        }

        public static void CreateConventionalCommandHandlerEndpoint<TMessage>(
            this IBusFactoryConfigurator configurator,
            Action<IReceiveEndpointConfigurator> configure) where TMessage : class
        {
            configurator.ReceiveEndpoint(typeof(TMessage).FullName.ToLower().Replace(".", "_"), configure);
        }

        public static void CreateConventionalCommandHandlerEndpoint<TConsumer, TMessage>(this IBusFactoryConfigurator cfg,IServiceProvider provider) where TMessage : class where TConsumer : class, IConsumer<TMessage>
        {
            cfg.ReceiveEndpoint(typeof(TMessage).FullName.ToLower().Replace(".", "_"),
                configurator =>
                {
                    
                    configurator.Consumer<TConsumer>(provider);
                    configurator.ConfigurePublish(pipeConfigurator =>
                    {
                        
                        pipeConfigurator.UseSendExecute(context =>
                        {
                            var consumeContext = context.GetPayload<ConsumeContext>();
                            context.TransferConsumeContextHeaders(consumeContext);
                        });
                    });

                    
                });
        }

        

        public static void CreateConventionalCommandMapping<TMessage>(this IHost host) where TMessage : class
        {
            var commandEndpoint = BuildConventionalAddress<TMessage>(host.Address.ToString());
            EndpointConvention.Map<TMessage>(new Uri(commandEndpoint));
        }
    }

    class CopyStuff : IPipeSpecification<SendContext>
    {
        public void Apply(IPipeBuilder<SendContext> builder)
        {
            builder.AddFilter(new StuffFilter());
        }

        public IEnumerable<ValidationResult> Validate()
        {
            return Enumerable.Empty<ValidationResult>();
        }
    }

    class StuffFilter : IFilter<SendContext>
    {
        public Task Send(SendContext context, IPipe<SendContext> next)
        {
            //context.TransferConsumeContextHeaders();
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context)
        {
            
        }
    }
}
