using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Control
{
  public class EventFuncsBuilder
  {
    public EventFuncsBuilder(IExecutionEnvironment executionEnvironment, IDomainEventFactory eventFactory)
    {
      _executionEnvironment = executionEnvironment;
      _eventFactory = eventFactory;
    }

    public Func<DomainEvent, DomainEvent[]>[] OnStateFuncs => _onStateFuncs.ToArray();
    public Func<DomainEvent, DomainEvent[]>[] OnEntryFuncs => _onEntryFuncs.ToArray();
    public Func<DomainEvent, DomainEvent[]>[] OnExitFuncs => _onExitFuncs.ToArray();

    public void AddOnEntry(Func<DomainEvent,Result<DomainEvent>> f)
    {
      _onEntryFuncs.Add(@event => ResultToDomainEventArray(f(@event)));
    }
    public void Setup(OnEntry onEntry)
    {
      SetupStartTimers(onEntry);
      SetupOnEntrySetValues(onEntry);
      SetupOnEntrySetProperties(onEntry);

      foreach (var setWithComputation in onEntry._setsWithComputations)
      {
        var functionContext = new FunctionContext(setWithComputation, _executionEnvironment.GetProperty);

        _onEntryFuncs.Add(@event =>
          Compute(setWithComputation._urnToSet, functionContext.Compute, _eventFactory));

        _onExitFuncs.Add(_ =>
        {
          functionContext.Reset();
          return _emptyResult;
        });
      }
    }
    public void Setup(OnState onState)
    {
      SetupOnStateSendCommandWithPropertyValue(onState);
      SetupSetWithConditions(onState);
      
      foreach (var setWithProperty in onState._setWithProperties)
      {
        _onStateFuncs.Add(@event =>
        {
          if (!(@event is PropertiesChanged propertiesChanged)) return _emptyResult;
          var changedProperty =
            propertiesChanged.ModelValues.FirstOrDefault(c => setWithProperty._propertyUrn.Equals(c.Urn));
          return changedProperty != null
            ? ResultToDomainEventArray(_eventFactory.NewEventResult(setWithProperty._urn,
              changedProperty.ModelValue()))
            : _emptyResult;
        });
      }

      foreach (var setWithComputation in onState._setWithComputations)
      {
        var functionContext = new FunctionContext(setWithComputation, _executionEnvironment.GetProperty);
        _onEntryFuncs.Add(@event =>
          Compute(setWithComputation._urnToSet, functionContext.Compute, _eventFactory));
        _onStateFuncs.Add(@event =>
          ComputeIfNeeded(@event, setWithComputation.TriggersUrn,
            () => Compute(setWithComputation._urnToSet, functionContext.Compute, _eventFactory)));
        _onExitFuncs.Add(_ =>
        {
          functionContext.Reset();
          return _emptyResult;
        });
      }

      foreach (var setPeriodical in onState._setPeriodical)
      {
        var functionContext = new FunctionContext(setPeriodical, _executionEnvironment.GetProperty);
        _onStateFuncs.Add(@event =>
          ComputePeriodically(@event,
            () => Compute(setPeriodical._urnToSet, functionContext.Compute, _eventFactory)));
        _onExitFuncs.Add(_ =>
        {
          functionContext.Reset();
          return _emptyResult;
        });
      }
    }
    
    public void Setup(OnExit onExit)
    {
      foreach (var setValue in onExit._setsValues)
      {
        _onExitFuncs.Add(_ => ResultToDomainEventArray(_eventFactory.NewEventResult(setValue._urn, setValue._value)));
      }

      foreach (var setWithProperty in onExit._setsWithProperty)
      {
        _onExitFuncs.Add(_ => ResultToDomainEventArray(
          from propertyValue in _executionEnvironment.GetProperty(setWithProperty._propertyUrn)
          from @event in _eventFactory.NewEventResult(setWithProperty._urn, propertyValue.ModelValue())
          select @event));
      }
    }
    
    private void SetupOnEntrySetValues(OnEntry onEntry)
    {
      foreach (var setValues in onEntry._setsValues)
      {
        _onEntryFuncs.Add(
          _ => ResultToDomainEventArray(_eventFactory.NewEventResult(setValues._urn, setValues._value)));
      }
    }
    
    private void SetupStartTimers(OnEntry onEntry)
    {
      foreach (var startTimer in onEntry._startTimers)
      {
        _onEntryFuncs.Add(_ =>
        {
          var timeoutRequestResult = 
            _eventFactory.NewEventResult(startTimer, default).Tap(it =>
              _executionEnvironment.SetTimeoutRequestInstance((NotifyOnTimeoutRequested) it));
          return ResultToDomainEventArray(timeoutRequestResult);
        });
      }
    }
    
    private void SetupOnStateSendCommandWithPropertyValue(OnState onState)
    {
      foreach (var setWithProperty in onState._setWithProperties)
      {
        _onEntryFuncs.Add(_ => ResultToDomainEventArray(
          from propertyValue in _executionEnvironment.GetProperty(setWithProperty._propertyUrn)
          from @event in _eventFactory.NewEventResult(setWithProperty._urn, propertyValue.ModelValue())
          select @event
        ));
      }
    }
    
    private void SetupOnEntrySetProperties(OnEntry onEntry)
    {
      foreach (var setWithProperty in onEntry._setsWithProperties)
      {
        _onEntryFuncs.Add(@event =>
          ResultToDomainEventArray(
            from property in _executionEnvironment.GetProperty(setWithProperty._propertyUrn)
            from newEvent in _eventFactory.NewEventResult(setWithProperty._urn, property.ModelValue())
            select newEvent)
        );
      }
    }
    
    private DomainEvent[] ComputeIfNeeded(DomainEvent @event, Urn[] triggers, Func<DomainEvent[]> compute)
    {
      if (@event is PropertiesChanged propertiesChanged)
        if (propertiesChanged.ModelValues.Select(c => c.Urn).Intersect(triggers).Any())
          return compute();

      return _emptyResult;
    }

    private DomainEvent[] ComputePeriodically(DomainEvent @event, Func<DomainEvent[]> compute)
    {
      if (@event is SystemTicked) return compute();
      return _emptyResult;
    }

    private DomainEvent[] Compute(Urn urnToSet, Func<Result<float>> functionContextCompute,
      IDomainEventFactory eventFactory) =>
      (from computedResult in functionContextCompute()
        select ResultToDomainEventArray(eventFactory.NewEventResult(urnToSet, computedResult)))
      .GetValueOrDefault(_emptyResult);

    private DomainEvent[] ResultToDomainEventArray(Result<DomainEvent> input) =>
      input.Match(_ => Array.Empty<DomainEvent>(), evt => new[] {evt});

    private void SetupSetWithConditions(OnState definition)
    {
      foreach (var setWithConditions in definition._setWithConditions.Values)
      {
        _onStateFuncs.Add(@event =>
        {
          if (!(@event is PropertiesChanged propertiesChanged)) return _emptyResult;
          if (!ShouldExecute(setWithConditions, propertiesChanged)) return _emptyResult;
          foreach (var with in setWithConditions.ConditionalsWith)
          {
            var conditionContext = new Control.Helpers.ConditionContext(_executionEnvironment.GetProperty, with._conditionDefinition);
            if (conditionContext.Execute())
              return CreateEvent(setWithConditions._setUrn, with);
          }

          return CreateEvent(setWithConditions._setUrn, setWithConditions.Otherwise);
        });
      }
    }
    
    private DomainEvent[] CreateEvent(Urn setUrn, With with) =>
      (with._isValueUrn
        ? _executionEnvironment.GetProperty(with._valueUrn)
          .Select(property => property.ModelValue())
          .SelectMany(
            value =>
              _eventFactory.NewEventResult(setUrn, value))
        : _eventFactory.NewEventResult(setUrn, with._value))
      .Match(_ => Array.Empty<DomainEvent>(), evt => new[] {evt});

    private static bool ShouldExecute(SetWithConditions setDefinition, PropertiesChanged propertiesChanged) =>
      setDefinition.TriggerUrns.Intersect(propertiesChanged.PropertiesUrns).Any();
    
    
    private readonly IExecutionEnvironment _executionEnvironment;
    private readonly IDomainEventFactory _eventFactory;
    private readonly DomainEvent[] _emptyResult = Array.Empty<DomainEvent>();

    private readonly List<Func<DomainEvent, DomainEvent[]>> _onStateFuncs = new List<Func<DomainEvent, DomainEvent[]>>();
    private readonly List<Func<DomainEvent, DomainEvent[]>> _onEntryFuncs = new List<Func<DomainEvent, DomainEvent[]>>();
    private readonly List<Func<DomainEvent, DomainEvent[]>> _onExitFuncs = new List<Func<DomainEvent, DomainEvent[]>>();


  }
}