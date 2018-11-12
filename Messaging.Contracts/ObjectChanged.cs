using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Contracts
{
    public interface ObjectCreated
    {
        string Id { get; set; }
        string SomeValue { get; set; }
    }

    public interface ObjectCreatedA : ObjectCreated
    {
        string A { get; set; }
    }

    public interface ObjectCreatedB : ObjectCreated
    {
        string B { get; set; }
    }
}
