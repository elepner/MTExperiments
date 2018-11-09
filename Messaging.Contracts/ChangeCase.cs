using System;

namespace Messaging.Contracts
{

    public interface ChangeCaseCommand
    {
        string Message { get; set; }
    }

    public class ChangeCaseCommandImpl : ChangeCaseCommand
    {
        public string Message { get; set; }
    }
}
