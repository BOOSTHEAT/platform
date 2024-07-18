using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Tools;

namespace ImpliciX.SharedKernel.Scheduling;

public class SchedulingUnit : IDisposable
{
    private IEventBusWithFirewall EventBus { get; }
    private readonly ManualResetEvent _applicationStarted;
    private readonly IImpliciXFeature _feature;
    private readonly Action<SchedulingUnit> _onStart;
    private readonly Action<SchedulingUnit> _onStop;
    private readonly ILog _logger;
    private readonly BlockingCollection<DomainEvent> _mailBox;
    private Thread _workerThread;
    private AutoResetEvent StopRequest { get; }
    public bool IsRunning => (_workerThread?.IsAlive).GetValueOrDefault();
    public string Id { get; }

    public SchedulingUnit(ManualResetEvent applicationStarted, string id, IImpliciXFeature feature,
        IEventBusWithFirewall eventBus, Action<SchedulingUnit> onStart = null, Action<SchedulingUnit> onStop = null,
        ILog logger = null)
    {
        EventBus = eventBus;
        _applicationStarted = applicationStarted;
        _feature = feature;
        _onStart = onStart;
        _onStop = onStop;
        _logger = logger ?? ImpliciXLogger.Create(new Dictionary<string, string>()
        {
            { "SchedulingUnit", Id }
        });
        _mailBox = new BlockingCollection<DomainEvent>();
        StopRequest = new AutoResetEvent(false);
        Id = id;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsRunning) return Task.CompletedTask;

        _logger.Information("Starting {@Id}", Id);
        SubscribeForHandlingEvents();
        Setup();
        _workerThread = new Thread(() => DoWork(cancellationToken)) { Name = $"{Id}" };
        _workerThread.Start();

        return Task.CompletedTask;
    }

    private void Setup()
    {
        _onStart?.Invoke(this);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.Information("Stopping {@Id}", Id);
        UnSubscribeForHandlingEvents();
        TearDown();
        StopRequest.Set();
        _workerThread.Join();
        _logger.Information("{@Id} is stopped. Working thread status : {@ThreadState}", Id, _workerThread.ThreadState);
        return Task.CompletedTask;
    }

    private void TearDown()
    {
        _onStop?.Invoke(this);
    }

    public void Post(DomainEvent @event)
    {
        if (_feature.CanExecute(@event))
            _mailBox.TryAdd(@event);

        if (_mailBox.Count > 100)
        {
            _logger.Warning("{@Id} Mailbox count {@Count}", Id, _mailBox.Count);
        }
    }

    public bool InMailBox(Predicate<DomainEvent> predicate) =>
        _mailBox.Any(evt => predicate(evt));

    private void DoWork(CancellationToken cancellationToken)
    {
        _logger.Debug("{@Id} is waiting for starting phase completion", Id);
        NativeThreadId = ThreadTools.GetCurrentThreadNativeId();
        _logger.Information("{@Id} has the native thread id {@ThreadId}", Id, NativeThreadId);
        _applicationStarted.WaitOne();
        _logger.Information("{@Id} has started to pump events on thread {@ThreadId}", Id, NativeThreadId);
        while (true)
        {
            if (StopRequest.WaitOne(0)) return;
            if (!SafeTryTake(cancellationToken, out var @event, 500))
                continue;

            // _logger.Verbose("{@Id} handling event type {@Name} on native thread {@ThreadId}", Id,
            //     @event.GetType().Name, NativeThreadId);
            SideEffect.TryRun(() =>
            {
                var sw = Stopwatch.StartNew();
                var resultingEvents = _feature.Execute(@event);
                sw.Stop();
                var elapsed = sw.ElapsedMilliseconds;
                if (elapsed > 500)
                {
                    _logger.Debug("{@Subsystem} work during {@elapses}ms", Id, elapsed);
                }

                foreach (var resultingEvent in resultingEvents)
                {
                    switch (resultingEvent)
                    {
                        case PrivateDomainEvent _:
                            Post(resultingEvent);
                            break;
                        case PublicDomainEvent _:
                            EventBus.Publish(Id, resultingEvent);
                            break;
                        default:
                            _logger.Error("{@EventType} is neither public or private",
                                resultingEvent.GetType().Name);
                            break;
                    }
                }
            }, exception =>
            {
                _logger.Error(exception, "{@Id} An unexpected error occured. {@message}. {@innerExceptionMessage}", Id, exception.Message,
                    exception?.InnerException?.Message);
                _logger.Debug("{@Id} An unexpected error occured. {@message}", Id, exception.ToString());
            });
        }
    }

    private ulong NativeThreadId { get; set; }

    private bool SafeTryTake(CancellationToken cancellationToken, out DomainEvent @event, int timeout = 0)
    {
        try
        {
            return _mailBox.TryTake(out @event, timeout, cancellationToken);
        }
        catch (Exception)
        {
            @event = null;
            return false;
        }
    }

    private void SubscribeForHandlingEvents()
    {
        foreach (var eventType in _feature.SupportedEvents)
        {
            EventBus.Subscribe(this, eventType, Post);
        }
    }

    private void UnSubscribeForHandlingEvents()
    {
        foreach (var eventType in _feature.SupportedEvents)
        {
            EventBus.UnSubscribe(this, eventType);
        }
    }

    public void Dispose()
    {
        StopRequest?.Dispose();
    }
}