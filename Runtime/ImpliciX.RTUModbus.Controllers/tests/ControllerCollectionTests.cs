using System;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RTUModbus.Controllers.Tests
{
    [TestFixture]
    public class ControllerCollectionTests
    {
        [Test]
        public void when_canhandle_of_one_slave_in_the_collection_fails_the_other_slaves_can_be_executed()
        {
            var succeedingController = new SucceedingController();
            var failingController = new FailingCanHandleController();
            var controllersCollection = TcpControllersCollection.Create(new ISlaveController[]{failingController,succeedingController});
            controllersCollection.Activate();
            controllersCollection.HandleDomainEvent(SystemTicked.Create(1000, 1));
            Check.That(succeedingController.Counter).IsEqualTo(1);
        }
        
        [Test]
        public void when_handle_domain_event_of_one_slave_in_the_collection_fails_the_other_slaves_can_be_executed()
        {
            var succeedingController = new SucceedingController();
            var failingController = new FailingDomainEventHandlerController();
            var controllersCollection = TcpControllersCollection.Create(new ISlaveController[]{failingController,succeedingController});
            controllersCollection.Activate();
            controllersCollection.HandleDomainEvent(SystemTicked.Create(1000, 1));
            Check.That(succeedingController.Counter).IsEqualTo(1);
        }
    }

    public class FailingDomainEventHandlerController : ISlaveController
    {
        public DomainEvent[] Activate()
        {
            return Array.Empty<DomainEvent>();
        }

        public bool CanHandle(DomainEvent trigger)
        {
            return true;
        }

        public DomainEvent[] HandleDomainEvent(DomainEvent trigger)
        {
            throw new Exception("boom");
        }
    }
    public class FailingCanHandleController : ISlaveController
    {
        public DomainEvent[] Activate()
        {
            return Array.Empty<DomainEvent>();
        }

        public bool CanHandle(DomainEvent trigger)
        {
            throw new Exception("boom");
        }

        public DomainEvent[] HandleDomainEvent(DomainEvent trigger)
        {
            throw new Exception("boom");
        }
    }

    public class SucceedingController : ISlaveController
    {
        public int Counter = 0;
        public DomainEvent[] Activate()
        {
            return Array.Empty<DomainEvent>();
        }

        public bool CanHandle(DomainEvent trigger)
        {
            return true;
        }

        public DomainEvent[] HandleDomainEvent(DomainEvent trigger)
        {
            Counter += 1;
            return Array.Empty<DomainEvent>();
        }
    }
}