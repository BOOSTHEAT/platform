using System;
using System.Collections.Generic;
using ImpliciX.Data.Factory;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using ImpliciX.ThingsBoard.Infrastructure;
using ImpliciX.ThingsBoard.Messages;
using ImpliciX.ThingsBoard.States;
using Moq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ThingsBoard.Tests.States
{
    [TestFixture]
    public class SendMessageTests
    {
        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(test_model).Assembly);
        }

        private static readonly Dictionary<string, (bool, Func<Type[]>)> TestCases =
            new Dictionary<string, (bool, Func<Type[]>)>
            {
                ["send-success"] = (true, () => Type.EmptyTypes),
                ["send-failure"] = (false, () => new[]
                {
                    typeof(ConnectToBroker.ConnectionFailed)
                })
            };

        [TestCase("send-success")]
        [TestCase("send-failure")]
        public void should_send_pending_messages_on_ticks(string caseName)
        {
            var (sendResult, expectedTypes) = TestCases[caseName];
            var azureIoTAdapter = new Mock<IMqttAdapter>();
            var clock = new VirtualClock(DateTime.Now);
            var context = new Context(string.Empty, new ThingsBoardSettings { GlobalRetries = 0 })
            {
                Adapter = azureIoTAdapter.Object
            };
            azureIoTAdapter.Setup(adapter => adapter.SendMessage(It.IsAny<IThingsBoardMessage>(), context)).Returns(sendResult);
            var queue = new Queue<IThingsBoardMessage>();
            var message = new Mock<IThingsBoardMessage>().Object;
            queue.Enqueue(message);
            var sut = Runner.CreateWithSingleState(context, new SendMessages(clock, queue, context));
            
            var propertyChanged = EventsHelper.EventSystemTicked(1000, TimeSpan.Zero);
            var domainEvents = sut.Handle(propertyChanged);
            
            Check.That(domainEvents.GetTypes()).IsEqualTo(expectedTypes());
            azureIoTAdapter.Verify(a => a.SendMessage(message, context), Times.Once);
        }

    }
}