using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Contracts
{
    public interface DoAnotherThingCommand
    {
        string ThingType { get; set; }
    }
}
