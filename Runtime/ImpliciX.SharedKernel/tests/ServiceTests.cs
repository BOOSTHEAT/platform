using System;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;
using static ImpliciX.SharedKernel.Bricks.Composables;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class ServiceTests
    {

        [Test]
        public void should_run_service()
        {
            DomainEvent[] domainEvents = { new MyDomainEvent1(), new MyDomainEvent2()};
            var sutService  = DomainEventHandler<MyTrigger>(trigger => domainEvents);
            var expectedEvents = sutService.Run(new MyTrigger());
            Check.That(expectedEvents).ContainsExactly(domainEvents);
        }

        [Test]
        public void it_is_lazy()
        {
            var wasExecuted = false;
            var sutService = DomainEventHandler<MyTrigger>(trigger =>
            {
                wasExecuted = true;
                return new DomainEvent[]{new MyDomainEvent1()};
            });
            Assert.IsFalse(wasExecuted);
            sutService.Run(new MyTrigger());
            Assert.IsTrue(wasExecuted);
        }
    }

    public class MyTrigger : PublicDomainEvent
    {
        public MyTrigger() : base(Guid.NewGuid(), TimeSpan.Zero)
        {
        }
    }

    public class MyDomainEvent1 : PublicDomainEvent
    {
        public MyDomainEvent1() : base(Guid.NewGuid(), TimeSpan.Zero)
        {
        }
    }

    public class MyDomainEvent2 : PublicDomainEvent
    {
        public MyDomainEvent2() : base(Guid.NewGuid(), TimeSpan.Zero)
        {
        }
    }
}
