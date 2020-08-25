// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOtherConsumer : Consumer<TestOtherBroker, TestOtherConsumerEndpoint, TestOffset>
    {
        public TestOtherConsumer(
            TestOtherBroker broker,
            TestOtherConsumerEndpoint endpoint,
            MessagesReceivedAsyncCallback callback,
            IReadOnlyCollection<IConsumerBehavior>? behaviors,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<TestOtherConsumer> logger)
            : base(broker, endpoint, callback, behaviors, serviceProvider, logger)
        {
        }

        public int AcknowledgeCount { get; set; }

        protected override void ConnectCore()
        {
        }

        protected override void DisconnectCore()
        {
        }

        protected override Task CommitCore(IReadOnlyCollection<TestOffset> offsets)
        {
            AcknowledgeCount += offsets.Count;
            return Task.CompletedTask;
        }

        protected override Task RollbackCore(IReadOnlyCollection<TestOffset> offsets)
        {
            // Nothing to do
            return Task.CompletedTask;
        }
    }
}
