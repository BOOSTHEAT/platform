using System;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.ThingsBoard.States
{
  public abstract class BaseState
  {
    protected BaseState(string name)
    {
      Name = name;
    }

    public StateDefinition<BaseState, (Context, DomainEvent), DomainEvent> Define() =>
      new StateDefinition<BaseState, (Context, DomainEvent), DomainEvent>(this, new[]
      {
        (Func<(Context, DomainEvent), DomainEvent[]>)(x => OnEntry(x.Item1, x.Item2))
      }, new[]
      {
        (Func<(Context, DomainEvent), DomainEvent[]>)(x => OnExit(x.Item1, x.Item2))
      }, new[]
      {
        (Func<(Context, DomainEvent), DomainEvent[]>)(x => OnState(x.Item1, x.Item2))
      });

    protected virtual DomainEvent[] OnEntry(Context context, DomainEvent @event) => Array.Empty<DomainEvent>();

    protected virtual DomainEvent[] OnExit(Context context, DomainEvent @event) => Array.Empty<DomainEvent>();

    protected virtual DomainEvent[] OnState(Context context, DomainEvent @event) => Array.Empty<DomainEvent>();

    public abstract bool CanHandle(Context context, DomainEvent @event);

    public string Name { get; }
  }
}