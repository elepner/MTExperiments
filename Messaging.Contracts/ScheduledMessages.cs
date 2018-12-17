using System;
using System.Collections.Generic;
using System.Text;

namespace Messaging.Contracts
{
    public interface ScheduledCommand
    {
        int ExecutedIn { get; set; }
        bool IsReallyScheduled { get; set; }
    }

    public class ScheduledCommandImpl : ScheduledCommand
    {
        public int ExecutedIn { get; set; }
        public bool IsReallyScheduled { get; set; }
    }

    public interface ExecuteActivity
    {
        string ActivityId { get; set; }
    }

    public class ExecuteActivityImpl : ExecuteActivity
    {
        public string ActivityId { get; set; }
    }
}
