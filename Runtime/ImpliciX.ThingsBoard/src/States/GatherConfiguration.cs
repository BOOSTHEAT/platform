using System;
using System.Linq;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.ThingsBoard.States
{
  public class GatherConfiguration : BaseState
  {
    public GatherConfiguration(ThingsBoardModuleDefinition moduleDefinition) : base(nameof(GatherConfiguration))
    {
      _moduleDefinition = moduleDefinition;
    }

    private readonly ThingsBoardModuleDefinition _moduleDefinition;

    protected override DomainEvent[] OnState(Context context, DomainEvent @event) =>
      @event is PropertiesChanged propertiesChanged
        ? Handle(context, propertiesChanged)
        : base.OnState(context, @event);

    public override bool CanHandle(Context _, DomainEvent @event) =>
      @event is PropertiesChanged || @event is GatheringComplete;

    public Transition<BaseState, (Context, DomainEvent)> WhenGatheringIsComplete(BaseState to)
      => new Transition<BaseState, (Context, DomainEvent)>(this, to, x => x.Item2 is GatheringComplete);

    public DomainEvent[] Handle(Context context, PropertiesChanged @event)
    {
      @event.GetPropertyValue<Literal>(_moduleDefinition.Connection.Host)
        .Tap(v => context.Host = v.ToString());

      @event.GetPropertyValue<Literal>(_moduleDefinition.Connection.AccessToken)
        .Tap(v => context.AccessToken = v.ToString());

      var gatheringComplete = new[]
      {
        context.Host,
        context.AccessToken
      }.All(v => !string.IsNullOrEmpty(v));

      return gatheringComplete ? new DomainEvent[] { new GatheringComplete(@event.At) } : new DomainEvent[0];
    }

    public class GatheringComplete : PrivateDomainEvent
    {
      public GatheringComplete(TimeSpan at) : base(Guid.NewGuid(), at)
      {
      }
    }
  }
}