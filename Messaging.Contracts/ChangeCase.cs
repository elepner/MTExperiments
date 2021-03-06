﻿using System;

namespace Messaging.Contracts
{

    public interface ChangeCaseCommand
    {
        string Text { get; set; }
        bool IsScheduled { get; set; }
    }

    public class ChangeCaseCommandImpl : ChangeCaseCommand
    {
        public string Text { get; set; }
        public bool IsScheduled { get; set; }
    }
}
