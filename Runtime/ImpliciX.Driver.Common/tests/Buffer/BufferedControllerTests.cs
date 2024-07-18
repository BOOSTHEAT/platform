using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Driver.Common.Buffer;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Driver.Common.Tests.Buffer
{
    [TestFixture]
    public class BufferedControllerTests
    {

        [Test]
        public void should_buffer_command_requested_events()
        {
            var buffer = new SpyCommandRequestedBuffer();
            var sut = CreateSut(buffer, _ => new DomainEvent[] {new DummyEvent()}, new StubClock());
            var trigger = CommandRequested.Create(fake_urn._setPoint, Temperature.Create(20), TimeSpan.Zero);
            var resultingEvents = sut(trigger);
            Check.That(resultingEvents).IsEmpty();
            Check.That(buffer.RecordedCommandRequested).ContainsExactly(trigger);
        }
        
        [Test]
        public void should_not_buffer_command_requested_events()
        {
            var buffer = new SpyCommandRequestedBuffer();
            var sut = CreateSut(buffer, _ => new DomainEvent[] {new DummyEvent()}, new StubClock());
            var trigger = new FizzEvent();
            var resultingEvents = sut(trigger);
            Check.That(resultingEvents).CountIs(1);
            Check.That(buffer.RecordedCommandRequested).IsEmpty();
        }
        
        [Test]
        public void should_release_buffered_command_requested_events_when_SystemTicked()
        {
            var bufferedCommand = CommandRequested.Create(fake_urn._setPoint, Temperature.Create(20), TimeSpan.Zero);
            var buffer = new SpyCommandRequestedBuffer(bufferedCommand);
            var recordedReleasedEvents = new List<DomainEvent>();
            var sut = CreateSut(buffer, SpyDomainEventHandler, new StubClock());
            
            var resultingEvents = sut(SystemTicked.Create(1000, 1));
            
            Check.That(recordedReleasedEvents).ContainsExactly(
                bufferedCommand,
                SystemTicked.Create(1000, 1));


            Check.That(resultingEvents.Select(e => e.GetType())).ContainsExactly(
                typeof(FizzEvent), typeof(BuzzEvent));

            DomainEvent[] SpyDomainEventHandler(DomainEvent @event)
            {
                recordedReleasedEvents.Add(@event);
                
                if (@event is SystemTicked) return new DomainEvent[] {new BuzzEvent()};
                return new[] {new FizzEvent()};
            }
        }

        [Test]
        public void should_skip_obsolete_SystemTicked()
        {
            var bufferedCommand = CommandRequested.Create(fake_urn._setPoint, Temperature.Create(20), TimeSpan.Zero);
            var buffer = new SpyCommandRequestedBuffer(bufferedCommand);
            var clock = VirtualClock.Create();

            clock.Advance(TimeSpan.FromSeconds(1));
            var oldSystemTicked = SystemTicked.Create(TimeSpan.Zero, 1000, 1);
            
            clock.Advance(TimeSpan.FromSeconds(2));
            
            var sut = CreateSut(buffer, _ => new DomainEvent[] {new DummyEvent()}, clock);
            var resultedEvents = sut(oldSystemTicked);
            Check.That(resultedEvents).IsEmpty();
        }
        


        private DomainEventHandler<DomainEvent> CreateSut(ICommandRequestedBuffer buffer, DomainEventHandler<DomainEvent> domainEventHandler, IClock clock) =>
            BufferedController.BufferedHandler(domainEventHandler, buffer, clock);


    }

    public class DummyEvent : PrivateDomainEvent
    {
        public DummyEvent() : base(Guid.Empty, TimeSpan.Zero)
        {
        }
    }
    
    public class FizzEvent : PrivateDomainEvent
    {
        public FizzEvent() : base(Guid.Empty, TimeSpan.Zero)
        {
        }
    }

    
    public class BuzzEvent : PrivateDomainEvent
    {
        public BuzzEvent() : base(Guid.Empty, TimeSpan.Zero)
        {
        }
    }
    
    
    
    public class SpyCommandRequestedBuffer : ICommandRequestedBuffer
    {
       
        public void ReceivedCommandRequested(CommandRequested commandRequested)
        {
            RecordedCommandRequested.Add(commandRequested);
        }

        public IEnumerable<CommandRequested> ReleaseCommandRequested()
        {
            return RecordedCommandRequested;
        }
        
        public List<CommandRequested> RecordedCommandRequested { get; }

        public SpyCommandRequestedBuffer()
        {
            RecordedCommandRequested = new List<CommandRequested>();
        }
        
        public SpyCommandRequestedBuffer(params CommandRequested[] bufferedCommands)
        {
            RecordedCommandRequested = new List<CommandRequested>(bufferedCommands);
        }
    }
}