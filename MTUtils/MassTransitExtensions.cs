using System;
using MassTransit;
using MassTransit.Azure.ServiceBus.Core;

namespace MTUtils
{
    public static class MassTransitExtensions
    {
        public static string BuildConventionalAddress<TMessage>(string hostName)
        {
            return $"{hostName}{typeof(TMessage).FullName.ToLower().Replace(".", "_")}";
        }

        public static void CreateConventionalCommandEndpoint<TMessage>(
            this IServiceBusBusFactoryConfigurator configurator, IServiceBusHost host,
            Action<IReceiveEndpointConfigurator> configure) where TMessage : class
        {
            configurator.ReceiveEndpoint(typeof(TMessage).FullName.ToLower().Replace(".", "_"), configure);
            
        }

        public static void CreateConventionalCommandMapping<TMessage>(this IServiceBusHost host) where TMessage : class
        {
            var commandEndpoint = BuildConventionalAddress<TMessage>(host.Address.ToString());
            EndpointConvention.Map<TMessage>(new Uri(commandEndpoint));
        }
    }
}
