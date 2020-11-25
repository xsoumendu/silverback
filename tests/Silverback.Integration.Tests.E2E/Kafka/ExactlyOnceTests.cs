﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ExactlyOnceTests : E2ETestFixture
    {
        public ExactlyOnceTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task ExactlyOnce_InMemoryInboundLog_DuplicatedMessagesIgnored()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1))
                                .AddInMemoryInboundLog())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        ExactlyOnceStrategy = ExactlyOnceStrategy.Log()
                                    }))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "1"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "2"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "1"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "2"
                });
            await publisher.PublishAsync(new TestEventOne());

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(4);

            Subscriber.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            Subscriber.InboundEnvelopes[1].Message.Should().BeOfType<TestEventWithUniqueKey>();
            Subscriber.InboundEnvelopes[1].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("1");
            Subscriber.InboundEnvelopes[2].Message.Should().BeOfType<TestEventWithUniqueKey>();
            Subscriber.InboundEnvelopes[2].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("2");
            Subscriber.InboundEnvelopes[3].Message.Should().BeOfType<TestEventOne>();

            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task ExactlyOnce_DbInboundLog_DuplicatedMessagesIgnored()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .UseDbContext<TestDbContext>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1))
                                .AddInboundLogDatabaseTable())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        ExactlyOnceStrategy = ExactlyOnceStrategy.Log()
                                    }))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>())
                .WithTestDbContext()
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();
            var dbContext = serviceProvider.GetRequiredService<TestDbContext>();

            await publisher.PublishAsync(new TestEventOne());
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "1"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "2"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "1"
                });
            await publisher.PublishAsync(
                new TestEventWithUniqueKey
                {
                    UniqueKey = "2"
                });
            await publisher.PublishAsync(new TestEventOne());

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();

            Subscriber.InboundEnvelopes.Should().HaveCount(4);

            Subscriber.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
            Subscriber.InboundEnvelopes[1].Message.Should().BeOfType<TestEventWithUniqueKey>();
            Subscriber.InboundEnvelopes[1].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("1");
            Subscriber.InboundEnvelopes[2].Message.Should().BeOfType<TestEventWithUniqueKey>();
            Subscriber.InboundEnvelopes[2].Message.As<TestEventWithUniqueKey>().UniqueKey.Should().Be("2");
            Subscriber.InboundEnvelopes[3].Message.Should().BeOfType<TestEventOne>();

            dbContext.InboundMessages.Count().Should().Be(4);
            DefaultTopic.GetCommittedOffsetsCount("consumer1").Should().Be(6);
        }

        [Fact]
        public async Task ExactlyOnce_InMemoryOffsetStore_DuplicatedMessagesIgnored()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1))
                                .AddInMemoryOffsetStore())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        ExactlyOnceStrategy = ExactlyOnceStrategy.OffsetStore()
                                    }))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>()
                        .AddDelegateSubscriber(
                            (KafkaPartitionsAssignedEvent message) =>
                                message.Partitions = message.Partitions.Select(
                                    topicPartitionOffset => new TopicPartitionOffset(
                                        topicPartitionOffset.TopicPartition,
                                        Offset.Beginning)).ToList()))
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 3"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            Subscriber.InboundEnvelopes.Should().HaveCount(3);

            var broker = serviceProvider.GetRequiredService<IBroker>();
            broker.Disconnect();
            broker.Connect();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 4"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            Subscriber.InboundEnvelopes.Should().HaveCount(4);
        }

        [Fact]
        public async Task ExactlyOnce_DbOffsetStore_DuplicatedMessagesIgnored()
        {
            var serviceProvider = Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .UseDbContext<TestDbContext>()
                        .WithConnectionToMessageBroker(
                            options => options
                                .AddMockedKafka(mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1))
                                .AddOffsetStoreDatabaseTable())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(new KafkaProducerEndpoint(DefaultTopicName))
                                .AddInbound(
                                    new KafkaConsumerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaConsumerConfig
                                        {
                                            GroupId = "consumer1",
                                            AutoCommitIntervalMs = 100
                                        },
                                        ExactlyOnceStrategy = ExactlyOnceStrategy.OffsetStore()
                                    }))
                        .AddSingletonSubscriber<OutboundInboundSubscriber>()
                        .AddDelegateSubscriber(
                            (KafkaPartitionsAssignedEvent message) =>
                                message.Partitions = message.Partitions.Select(
                                    topicPartitionOffset => new TopicPartitionOffset(
                                        topicPartitionOffset.TopicPartition,
                                        Offset.Beginning)).ToList()))
                .WithTestDbContext()
                .Run();

            var publisher = serviceProvider.GetRequiredService<IEventPublisher>();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 1"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 2"
                });
            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 3"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            Subscriber.InboundEnvelopes.Should().HaveCount(3);

            var broker = serviceProvider.GetRequiredService<IBroker>();
            broker.Disconnect();
            broker.Connect();

            await publisher.PublishAsync(
                new TestEventOne
                {
                    Content = "Message 4"
                });

            await KafkaTestingHelper.WaitUntilAllMessagesAreConsumedAsync();
            Subscriber.InboundEnvelopes.Should().HaveCount(4);
        }
    }
}
