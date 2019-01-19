﻿// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class SimpleEvent : IEvent
    {
        public string Content { get; set; }
    }
}