using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using ImpliciX.ThingsBoard.States;

namespace ImpliciX.ThingsBoard
{
  public class Runner
  {
    public Runner(Context context, BaseState initialState,
      StateDefinition<BaseState, (Context, DomainEvent), DomainEvent>[] stateDefinitions,
      params Transition<BaseState, (Context, DomainEvent)>[] transitions)
    {
      CurrentState = initialState;
      Context = context;
      _fsm = new FSM<BaseState, (Context, DomainEvent), DomainEvent>(initialState, stateDefinitions, transitions);
    }

    public DomainEvent[] Activate()
    {
      var (nextState, output) =
        _fsm.Activate((Context, null), state => Log.Information("ThingsBoard is now {0}", state));
      CurrentState = nextState;
      return output;
    }

    public DomainEvent[] Handle(DomainEvent input)
    {
      var (nextState, output) = _fsm.TransitionFrom(CurrentState,
        (Context, input),
        stateChanged: state =>
        {
          CurrentState = (BaseState)state;
          Log.Information("ThingsBoard is now {0}", CurrentState.Name);
        });

      return output;
    }

    public bool CanHandle(DomainEvent input) => CurrentState.CanHandle(Context, input);

    private BaseState CurrentState { get; set; }
    public Context Context { get; }
    private readonly FSM<BaseState, (Context, DomainEvent), DomainEvent> _fsm;

    public static Runner CreateWithSingleState(Context context, BaseState singleState) =>
      new Runner(context, singleState, new[] { singleState.Define() });
  }
}