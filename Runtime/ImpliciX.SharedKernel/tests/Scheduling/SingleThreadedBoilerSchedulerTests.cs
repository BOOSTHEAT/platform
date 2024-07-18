using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Scheduling;
using ImpliciX.SharedKernel.Tools;
using NFluent;
using NUnit.Framework;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.SharedKernel.Tests.Scheduling
{
    [TestFixture]
    public class SingleThreadedSchedulerTests
    {
        [Test]
        public void nominal_case()
        {
            var testEnd = new AutoResetEvent(false);
            var modules = new IImpliciXModule[] {new ModuleA(), new ModuleB(1, testEnd), SpyModule};
            var sut = new SingleThreadedScheduler(ApplicationStarted, EventBus, modules, RealClock.Create(),new TestDependencyProvider());

            EventBus.SignalApplicationStarted();
            ApplicationStarted.Set();
            EventBus.Publish(nameof(SingleThreadedSchedulerTests), new DomainEventX(2));
            sut.StartAsync(CancellationSource.Token).GetAwaiter().GetResult();
            testEnd.WaitOne();
            sut.StopAsync(CancellationSource.Token).GetAwaiter().GetResult();
            Check.That(SpyModule.RecordedDomainEvents.Take(5).Select(e => e.GetType())).ContainsExactly(typeof(DomainEventX),
                typeof(DomainEventX), typeof(Idle), typeof(DomainEventY), typeof(Idle));
        }

        [SetUp]
        public void Setup()
        {
            ApplicationStarted = new ManualResetEvent(false);
            EventBus = EventBusWithFirewall.CreateWithFirewall();
            CancellationSource = new CancellationTokenSource();
            SpyModule = new SpyModule();
        }

        public ManualResetEvent ApplicationStarted { get; set; }

        public SpyModule SpyModule { get; set; }
        public CancellationTokenSource CancellationSource { get; set; }

        public EventBusWithFirewall EventBus { get; set; }
    }

    public class ModuleA : ImpliciXModule
    {
        public ModuleA() : base("ModuleA")
        {
            DefineModule(
                initDependencies: _ => { },
                initResources: _ => new object[0], 
                createFeature: provider =>
                {
                   return DefineFeature().Handles<DomainEventX>(dx =>
                    {
                        return dx.X > 0
                            ? Enumerable.Range(0, dx.X - 1).Select(x => new DomainEventX(x)).Cast<DomainEvent>().ToArray()
                            : new DomainEvent[0];
                    }).Create();
                }
            );
        }
    }

    public class ModuleB : ImpliciXModule
    {
        private uint _limit;
        private readonly AutoResetEvent _testEnd;

        public ModuleB(uint limit, AutoResetEvent testEnd) : base("ModuleB")
        {
            _limit = limit;
            _testEnd = testEnd;
            DefineModule(
                initDependencies: _ => { },
                initResources: _ => new object[0],
                createFeature: provider =>
                {
                    return DefineFeature()
                        .Handles<DomainEventX>(dx => new DomainEvent[0])
                        .Handles<Idle>(dy =>
                        {
                            if (_limit-- == 0)
                            {
                                _testEnd.Set();
                                return new DomainEvent[0];
                            }
                            return new DomainEvent[] {new DomainEventY(19)};
                        })
                        .Create();
                }
            );
        }
    }

    public class SpyModule : ImpliciXModule
    {
        public SpyModule() : base("Spy")
        {
            RecordedDomainEvents = new List<DomainEvent>();
            DefineModule(
                initDependencies: _ => { },
                initResources: _ => new object[0],
                createFeature: provider =>
                {
                    return DefineFeature()
                        .Handles<DomainEventX>(dx =>
                        {
                            RecordedDomainEvents.Add(dx);
                            return new DomainEvent[0];
                        })
                        .Handles<DomainEventY>(dy =>
                        {
                            RecordedDomainEvents.Add(dy);
                            return new DomainEvent[0];
                        })
                        .Handles<Idle>(dz =>
                        {
                            RecordedDomainEvents.Add(dz);
                            return new DomainEvent[0];
                        })
                        .Create();
                }
            );
        }

        public List<DomainEvent> RecordedDomainEvents { get; }
    }

    public class TestDependencyProvider : IProvideDependency
    {
        public T GetService<T>() => 
            default(T);

        public T GetSettings<T>(string moduleId) where T : class, new() => 
            GetService<T>();
    }
    public class DomainEventX : PublicDomainEvent
    {
        public int X { get; }

        public DomainEventX(int x) : base(Guid.NewGuid(), TimeSpan.Zero)
        {
            X = x;
        }
    }

    public class DomainEventY : PublicDomainEvent
    {
        public int Y { get; }

        public DomainEventY(int y) : base(Guid.NewGuid(), TimeSpan.Zero)
        {
            Y = y;
        }
    }
}