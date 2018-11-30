using System;
using System.Threading.Tasks;
using MassTransit;
using Messaging.Contracts;

namespace MTExperiments.Consumer
{
    public class ChangeCaseCommandHandler : IConsumer<ChangeCaseCommand>
    {
        private static int Count = 0;

        public async Task Consume(ConsumeContext<ChangeCaseCommand> context)
        {
            if (Count++ % 4 == 0)
            {
                throw new Exception("Cannot change case so often!!");
            }

            string message = context.Message.Text;
            string result = "";
            foreach (char c in message)
            {
                char changedCase;
                if (char.IsUpper(c))
                {
                    changedCase = char.ToLower(c);
                }
                else if (char.IsLower(c))
                {
                    changedCase = char.ToUpper(c);
                }
                else
                {
                    changedCase = c;
                }

                result += changedCase;
                //Console.Write(toPrint);
            }
            Console.WriteLine(result);

            await context.Publish<AnotherThingHappened>(new AnotherThing
            {
                AnotherThingType = $"Changed case from {context.Message.Text} to {result}"
            });
        }
    }

    class AnotherThing : AnotherThingHappened
    {
        public string AnotherThingType { get; set; }
    }
}