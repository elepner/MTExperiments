using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GreenPipes;
using GreenPipes.Specifications;
using MassTransit;
using MassTransit.Scheduling;


namespace MTUtils
{
    public static class MassTransitExtensions
    {
        public const string TENANT_ID_KEY = "TenantId";

        public static string BuildConventionalPathForType(Type commandType)
        {
            return $"{commandType.Namespace.Replace(".", "_")}/{commandType.Name.Replace(".", "_")}";
        }

        public static string BuildConventionalAddress<TMessage>(string hostName)
        {
            return $"{hostName}{BuildConventionalPathForType(typeof(TMessage))}";
        }

        public static void CreateConventionalCommandHandlerEndpoint<TMessage>(
            this IBusFactoryConfigurator configurator,
            Action<IReceiveEndpointConfigurator> configure) where TMessage : class
        {
            configurator.ReceiveEndpoint(typeof(TMessage).FullName.ToLower().Replace(".", "_"), configure);
        }

        public static void CreateConventionalCommandHandlerEndpoint<TConsumer, TMessage>(this IBusFactoryConfigurator cfg,IServiceProvider provider) where TMessage : class where TConsumer : class, IConsumer<TMessage>
        {
            cfg.ReceiveEndpoint(BuildConventionalPathForType(typeof(TMessage)),
                configurator =>
                {
                    configurator.Consumer<TConsumer>(provider);
                });
        }

        public static void CreateConventionalCommandMapping<TMessage>(this IHost host) where TMessage : class
        {
            var commandEndpoint = BuildConventionalAddress<TMessage>(host.Address.ToString());
            EndpointConvention.Map<TMessage>(new Uri(commandEndpoint));
        }

        public static void ConfigureExtraHeadersCopying(this IBusFactoryConfigurator cfg)
        {
            cfg.ConfigureSend(configurator =>
            {
                configurator.UseSendExecute(context =>
                {
                    if (context.TryGetPayload(out ConsumeContext consumeContext))
                    {
                        context.TransferConsumeContextHeaders(consumeContext);
                    }
                });
            });

            cfg.ConfigurePublish(configurator =>
            {
                configurator.UseSendExecute(context =>
                {
                    if (context.TryGetPayload(out ConsumeContext consumeContext))
                    {
                        context.TransferConsumeContextHeaders(consumeContext);
                    }
                });
            });
        }

        public static Task<ScheduledMessage<T>> ScheduleSendConventional<T>(this ConsumeContext context, TimeSpan delay,
            T message, CancellationToken cancellationToken = default(CancellationToken)) where T : class
        {
            var destAddress = EndpointConvention.TryGetDestinationAddress<T>(out var desAddress);
            if (!destAddress)
            {
                throw new Exception("Mapping is not found");
            }
            return context.ScheduleSend(
                desAddress,
                delay, message, cancellationToken);
        }


    }
}
