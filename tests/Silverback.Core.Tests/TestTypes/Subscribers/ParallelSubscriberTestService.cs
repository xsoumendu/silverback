﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class ParallelSubscriberTestService : ISubscriber
    {
        public ParallelTestingUtil Parallel { get; } = new ParallelTestingUtil();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [Subscribe(Parallel = true)]
        private void OnMessageReceived(object message) => Parallel.DoWork();

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        [SuppressMessage("ReSharper", "UnusedParameter.Local", Justification = Justifications.CalledBySilverback)]
        [Subscribe(Parallel = true)]
        private Task OnMessageReceivedAsync(object message) => Parallel.DoWorkAsync();
    }
}