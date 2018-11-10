using System;
using System.Threading.Tasks;
using MassTransit;
using Messaging.Contracts;

namespace MTExperiments.Consumer
{
    public class ChangeCaseCommandHandler : IConsumer<ChangeCaseCommand>
    {
        public Task Consume(ConsumeContext<ChangeCaseCommand> context)
        {
            string message = context.Message.Text;
            foreach (char c in message)
            {
                char toPrint;
                if (char.IsUpper(c))
                {
                    toPrint = char.ToLower(c);
                }
                else if (char.IsLower(c))
                {
                    toPrint = char.ToUpper(c);
                }
                else
                {
                    toPrint = c;
                }

                Console.Write(toPrint);
            }
            Console.WriteLine();
            return Task.CompletedTask;
        }
    }
}