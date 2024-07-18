using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Scheduling;
using ImpliciX.SharedKernel.Tests.Doubles;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.SharedKernel.Tests.Scheduling
{
    [TestFixture]
    public class SchedulingUnitTests
    {
        [Test]
        public void life_cycle_nominal_case()
        {
            var handlingResetEvent = new AutoResetEvent(false);
            var applicationStarted = new ManualResetEvent(false);
            var spyActions = new SpyActions();
            var spyEventBus = new SpyEventBus();
            var sut = SchedulingUnitNominalCase(applicationStarted, handlingResetEvent, spyActions, spyEventBus);
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            sut.StartAsync(token).GetAwaiter().GetResult();
            applicationStarted.Set();
            sut.Post(new FooEvent());
            handlingResetEvent.WaitOne(500);
            sut.StopAsync(token).GetAwaiter().GetResult();

            Check.That(spyActions.RecordedEvents).ContainsExactly("starting", "FooEvent", "stopping");
            Check.That(spyEventBus.RecordedSubscriptions).ContainsExactly(typeof(FooEvent));
            Check.That(spyEventBus.RecordedUnSubscriptions).ContainsExactly(typeof(FooEvent));
            Check.That(spyEventBus.RecordedPublications.Select(p => p.GetType()))
                .ContainsExactly(typeof(BarEvent), typeof(FooEvent));
            Assert.IsFalse(sut.IsRunning);
        }

        [Test]
        public void when_event_has_no_handler()
        {
            var handlingResetEvent = new AutoResetEvent(false);
            var applicationStarted = new ManualResetEvent(false);
            var spyActions = new SpyActions();
            var spyEventBus = new SpyEventBus();
            var sut = SchedulingUnitNominalCase(applicationStarted, handlingResetEvent, spyActions, spyEventBus);
            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            sut.StartAsync(token).GetAwaiter().GetResult();
            applicationStarted.Set();
            sut.StopAsync(token).GetAwaiter().GetResult();
        
            Check.ThatCode(() => sut.Post(new UnknownEvent())).DoesNotThrow();
        }

        [Test]
        public void when_exception_is_raised_while_handling_an_event_it_should_not_crash_and_continue_to_handle_events()
        {
            var handlingResetEvent = new AutoResetEvent(false);
            var applicationStarted = new ManualResetEvent(false);
            var spyActions = new SpyActions();
            var spyEventBus = new SpyEventBus();
            var sut = SchedulingUnitFailingWhenHandlingMessage(applicationStarted, handlingResetEvent, spyActions,
                spyEventBus);
            sut.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            applicationStarted.Set();
            sut.Post(new BarEvent());
            sut.Post(new FooEvent());
            handlingResetEvent.WaitOne(500);
            sut.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

            var expectedEvents = new List<string>() {"starting", "FooEvent", "stopping"};
            Assert.AreEqual(expectedEvents, spyActions.RecordedEvents);
        }

        [Test]
        public void mailbox_can_be_inspected()
        {
            var handlingResetEvent = new AutoResetEvent(false);
            var applicationStarted = new ManualResetEvent(false);
            var spyActions = new SpyActions();
            var spyEventBus = new SpyEventBus();
            var sut = SchedulingUnitFailingWhenHandlingMessage(applicationStarted, handlingResetEvent, spyActions,
                spyEventBus);
            sut.Post(new BarEvent());
            Check.That(sut.InMailBox(evt => evt is BarEvent)).IsTrue();
            Check.That(sut.InMailBox(evt => evt is FooEvent)).IsFalse();
        }

        [Test]
        public void many_handlers_can_be_defined_for_the_same_event()
        {
            var handlingResetEvent = new AutoResetEvent(false);
            var applicationStarted = new ManualResetEvent(false);
            var spyActions = new SpyActions();
            var spyEventBus = new SpyEventBus();
            var sut = SchedulingUnitFeatureWithTwoHandlersForTheSameEvent(applicationStarted, handlingResetEvent, spyActions, spyEventBus);
            sut.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            applicationStarted.Set();
            sut.Post(new FooEvent(){Foo = 1});
            handlingResetEvent.WaitOne(500);
            handlingResetEvent.Reset();
            sut.Post(new FooEvent(){Foo = 2});
            handlingResetEvent.WaitOne(500);
            sut.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

            Check.That(spyEventBus.RecordedPublications.Select(e=>e.GetType()))
                .Contains(typeof(BarEvent),typeof(FizzEvent));
        }
        
        [Test]
        public void should_publish_directly_in_the_mailbox_only_private_events()
        {
            var handlingFooEvent = new AutoResetEvent(false);
            var handlingBazEvent = new AutoResetEvent(false);
            var applicationStarted = new ManualResetEvent(false);
            var spyActions = new SpyActions();
            var spyEventBus = new SpyEventBus();
            var sut = SchedulingUnitNominalCaseWithPrivateMessage(applicationStarted, handlingFooEvent, handlingBazEvent, spyActions, spyEventBus);

            var cancellationTokenSource = new CancellationTokenSource();
            var token = cancellationTokenSource.Token;
            sut.StartAsync(token).GetAwaiter().GetResult();
            applicationStarted.Set();
            sut.Post(new FooEvent());
            handlingFooEvent.WaitOne(500);
            handlingBazEvent.WaitOne(500);
            sut.StopAsync(token).GetAwaiter().GetResult();
            var recordedTypesOnBus = spyEventBus.RecordedPublications.Select(p => p.GetType()).ToArray();
            Check.That(recordedTypesOnBus).ContainsExactly(typeof(BarEvent), typeof(FizzEvent));
            Check.That(recordedTypesOnBus).Not.Contains(typeof(BazEvent));
        }

        private static SchedulingUnit SchedulingUnitFailingWhenHandlingMessage(ManualResetEvent applicationStarted,
            AutoResetEvent handlingResetEvent, SpyActions spyActions, IEventBusWithFirewall spyEventBus)
        {
            var executable = DefineFeature()
                .Handles<BarEvent>(HandlerThrowingException(handlingResetEvent, spyActions))
                .Handles<FooEvent>(FooHandler(handlingResetEvent, spyActions, new BarEvent(), new FooEvent()))
                .Create();
            return new SchedulingUnit(applicationStarted, "AgentFailingCase", executable, spyEventBus,
                _ => spyActions.Record("starting"), _ => spyActions.Record("stopping"));
        }

        private static SchedulingUnit SchedulingUnitNominalCase(ManualResetEvent applicationStarted,
            AutoResetEvent handlingResetEvent, SpyActions spyActions, IEventBusWithFirewall spyEventBus)
        {
            var executable = DefineFeature()
                .Handles<FooEvent>(FooHandler(handlingResetEvent, spyActions, new BarEvent(), new FooEvent()))
                .Create();
            return new SchedulingUnit(applicationStarted, "AgentNominalCase", executable, spyEventBus,
                _ => spyActions.Record("starting"), _ => spyActions.Record("stopping"));
        }

        private static SchedulingUnit SchedulingUnitNominalCaseWithPrivateMessage(ManualResetEvent applicationStarted,
            AutoResetEvent handlingFooEvent, AutoResetEvent handlingBazEvent, SpyActions spyActions, IEventBusWithFirewall spyEventBus)
        {
            var executable = DefineFeature()
                .Handles<FooEvent>(FooHandler(handlingFooEvent, spyActions, new BarEvent(), new BazEvent()))
                .Handles<BazEvent>(BazHandler(handlingBazEvent, spyActions, new FizzEvent()))
                .Create();
            return new SchedulingUnit(applicationStarted, "AgentNominalCase", executable, spyEventBus,
                _ => spyActions.Record("starting"), _ => spyActions.Record("stopping"));
        }
        
        
        private static SchedulingUnit SchedulingUnitFeatureWithTwoHandlersForTheSameEvent(ManualResetEvent applicationStarted,
            AutoResetEvent handlingFooEvent, SpyActions spyActions, IEventBusWithFirewall spyEventBus)
        {
            var executable = DefineFeature()
                .Handles<FooEvent>(FooHandler(handlingFooEvent, spyActions, new BarEvent()),foo=>foo.Foo==1)
                .Handles<FooEvent>(FooHandler(handlingFooEvent, spyActions, new FizzEvent()),foo=>foo.Foo==2)
                .Create();
            return new SchedulingUnit(applicationStarted, "AgentNominalCase", executable, spyEventBus,
                _ => spyActions.Record("starting"), _ => spyActions.Record("stopping"));
        }
        

        private static DomainEventHandler<BarEvent> HandlerThrowingException(AutoResetEvent handlingResetEvent,
            SpyActions spyActions)
            => (@event) =>
            {
                if (@event is BarEvent) throw new Exception("boom");
                return new DomainEvent[] { };
            };

        private static DomainEventHandler<FooEvent> FooHandler(AutoResetEvent handlingResetEvent,
            SpyActions spyActions, params DomainEvent[] outputEvents) =>
            (@event) =>
            {
                spyActions.Record(@event.GetType().Name);
                handlingResetEvent.Set();
                return outputEvents;
            };
        
        private static DomainEventHandler<BazEvent> BazHandler(AutoResetEvent handlingResetEvent,
            SpyActions spyActions, params DomainEvent[] outputEvents) =>
            (@event) =>
            {
                spyActions.Record(@event.GetType().Name);
                handlingResetEvent.Set();
                return outputEvents;
            };
    }
}