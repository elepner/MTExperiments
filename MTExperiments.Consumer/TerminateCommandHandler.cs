using System.Threading;
using System.Threading.Tasks;
using MassTransit;
using Messaging.Contracts;
using Microsoft.Extensions.Hosting;

namespace MTExperiments.Consumer
{
    public class TerminateCommandHandler : IConsumer<TerminateCommand>
    {
        private readonly IHostLifetime _hostLifetime;


        public TerminateCommandHandler(IHostLifetime hostLifetime)
        {
            _hostLifetime = hostLifetime;
        }
        public async Task Consume(ConsumeContext<TerminateCommand> context)
        {
            await _hostLifetime.StopAsync(CancellationToken.None);
            Program.TaskCompletionSource.SetResult(1);
        }
    }
}