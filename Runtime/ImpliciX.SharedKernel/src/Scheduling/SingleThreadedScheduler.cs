using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace ImpliciX.SharedKernel.Scheduling
{
    public class SingleThreadedScheduler : ImpliciXScheduler
    {
        private IClock _clock;
        private IImpliciXFeature[] _features;
        private ConcurrentQueue<DomainEvent> _mailBox;
        private IImpliciXModule[] _modules;
        private Thread _workerThread;
        private AutoResetEvent _stopRequest;
        private ManualResetEvent _applicationStarted;
        private IEventBusWithFirewall _eventBus;
        private DomainEvent _lastProcessedEvent = new Idle(TimeSpan.Zero, 0, 0);
        private TimeSpan _mailboxDrainingStartTime;
        private TimeSpan _idleCycleStartTime;


        public SingleThreadedScheduler(ManualResetEvent applicationStarted, IServiceProvider serviceProvider,
            IImpliciXModule[] modules,
            IClock clock)
        {
            Init(applicationStarted, serviceProvider.GetService<IEventBusWithFirewall>(), modules, clock, new DependencyProvider(serviceProvider));
        }

        public SingleThreadedScheduler(ManualResetEvent applicationStarted, IEventBusWithFirewall eventBus,
            IImpliciXModule[] modules, IClock clock, IProvideDependency dependencyProvider)
        {
            Init(applicationStarted, eventBus, modules, clock, dependencyProvider);
        }

        private void Init(ManualResetEvent applicationStarted, IEventBusWithFirewall eventBus, IImpliciXModule[] modules, IClock clock,
            IProvideDependency dependencyProvider)
        {
            _clock = clock;
            _eventBus = eventBus;
            _mailBox = new ConcurrentQueue<DomainEvent>();
            _stopRequest = new AutoResetEvent(false);
            _applicationStarted = applicationStarted;
            _modules = modules;
            _modules.InitializeResources(dependencyProvider);

            _features = modules.Select(m => m.Feature).ToArray();
            var supportedEvents = _features.SelectMany(f => f.SupportedEvents).Distinct();
            foreach (var supportedEvent in supportedEvents)
            {
                eventBus.Subscribe(this, supportedEvent, EnqueueEvent);
            }
        }

        private void EnqueueEvent(DomainEvent d)
        {
            if (_mailBox.Count > 100)
            {
                Log.Warning("Single threaded scheduler mailbox count {@Count}", _mailBox.Count);
            }

            _mailBox.Enqueue(d);
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _workerThread = new Thread(() => DoWork(cancellationToken)) { Name = "SingleThreadedScheduler" };
            _workerThread.Start();
            Log.Information("Starting single threaded scheduler {@ModuleIds}", _modules.Select(m => m.Id).ToArray());
            return Task.CompletedTask;
        }

        private void DoWork(CancellationToken _)
        {
            NativeThreadId = ThreadTools.GetCurrentThreadNativeId();
            Log.Information("Single threaded scheduler has the native thread id {@ThreadId}", NativeThreadId);
            _applicationStarted.WaitOne();
            _mailboxDrainingStartTime = _clock.Now();
            _idleCycleStartTime = _clock.Now();
            while (!_stopRequest.WaitOne(0))
            {
                if (_mailBox.TryDequeue(out var @event))
                {
                    ComputeMailboxDrainingStartTime(@event, _clock.Now());

                    BulkEnqueue(Work(@event));
                }
                else
                {
                    var drainingDuration = Convert.ToInt32((_clock.Now() - _mailboxDrainingStartTime).TotalMilliseconds);
                    var idleCycleDuration = Convert.ToInt32((_clock.Now() - _idleCycleStartTime).TotalMilliseconds);
                    _idleCycleStartTime = _clock.Now();
                    BulkEnqueue(new DomainEvent[] { new Idle(_clock.Now(), drainingDuration, idleCycleDuration) });
                }
            }
        }

        private void ComputeMailboxDrainingStartTime(DomainEvent currentEvent, TimeSpan currentTime)
        {
            if (_lastProcessedEvent is Idle && !(currentEvent is Idle))
            {
                _mailboxDrainingStartTime = currentTime;
            }

            _lastProcessedEvent = currentEvent;
        }

        private ulong NativeThreadId { get; set; }

        private void BulkEnqueue(DomainEvent[] domainEvents)
        {
            _eventBus.Publish(domainEvents);
        }

        private DomainEvent[] Work(DomainEvent @event)
        {
            return _features.SelectMany(f =>
            {
                try
                {
                    return f.Execute(@event);
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "An unexpected error occured.");
                    return Array.Empty<DomainEvent>();
                }
            }).ToArray();
        }


        public override Task StopAsync(CancellationToken cancellationToken)
        {
            Log.Information("Stopping single threaded scheduler");
            _stopRequest.Set();
            _workerThread.Join();
            return Task.CompletedTask;
        }
    }
}