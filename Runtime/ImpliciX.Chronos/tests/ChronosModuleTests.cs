using System;
using System.Threading;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Scheduling;
using ImpliciX.SharedKernel.Tools;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Chronos.Tests
{
    [TestFixture]
    public class ChronosModuleTests
    {
        private const int BASE_FREQUENCY = 1000;

        [Test]
        public void should_send_periodic_system_ticked_events()
        {
            var startTime = new DateTime(2003, 10, 8, 12, 0, 0, DateTimeKind.Utc);
            var clock = VirtualClock.Create(startTime);
            var spyBus = new SpyEventBus();
            var applicationStarted = new ManualResetEvent(false);
            var sut = MultiThreadedSchedulerWithChronosModule(spyBus, clock, applicationStarted);
            sut.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
            applicationStarted.Set();
            clock.Advance(TimeSpan.FromSeconds(1));
            clock.Advance(TimeSpan.FromSeconds(1));
            sut.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
            var expectedEvents = new[]
            {
                SystemTicked.Create(new TimeSpan(startTime.Ticks), BASE_FREQUENCY, 1),
                SystemTicked.Create(new TimeSpan(startTime.Ticks), BASE_FREQUENCY, 2),
            };
            Check
                .That(spyBus.RecordedPublications.FilterEvents<SystemTicked>())
                .ContainsExactly(expectedEvents);
        }

        private static MultiThreadedScheduler MultiThreadedSchedulerWithChronosModule(SpyEventBus spyBus, VirtualClock clock,
            ManualResetEvent applicationStarted)
        {
            var chronosModule = new ChronosModule("Chronos");
            chronosModule.InitializeResources(new StubDependencyProvider(spyBus, clock));
            var schedulingUnit = new SchedulingUnit(applicationStarted, chronosModule.Id, chronosModule.Feature, spyBus,
                chronosModule.OnStartSchedulingUnitAction, chronosModule.OnStopSchedulingUnitAction);
            return new MultiThreadedScheduler(spyBus, new SchedulingUnit[] { schedulingUnit });
        }

        private class StubDependencyProvider : IProvideDependency
        {
            private readonly IEventBusWithFirewall _bus;
            private readonly IClock _clock;

            public StubDependencyProvider(IEventBusWithFirewall bus, IClock clock)
            {
                _bus = bus;
                _clock = clock;
            }

            public T GetService<T>()
            {
                object obj = typeof(T) switch
                {
                    var t when t == typeof(IEventBusWithFirewall) => _bus,
                    var t when t == typeof(IClock) => _clock,
                    _ => throw new NotSupportedException()
                };
                return (T) obj;
            }

            public T GetSettings<T>(string moduleId) where T : class, new() =>
                (new ChronosSettings() { BasePeriodMilliseconds = BASE_FREQUENCY }) as T;
        }
    }
}