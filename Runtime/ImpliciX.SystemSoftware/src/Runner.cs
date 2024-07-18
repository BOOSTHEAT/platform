using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;
using ImpliciX.SystemSoftware.States;

namespace ImpliciX.SystemSoftware
{
    public class Runner<CONTEXT>
    {
        private const string ModuleName = "SystemUpdate";


        public DomainEvent[] Activate()
        {
            var (nextState, output) = _fsm.Activate((Context, null));
            CurrentState = nextState;
            Log.Information("{0} is now {1}", ModuleName, CurrentState);
            return output;
        }

        public DomainEvent[] Handle(DomainEvent input)
        {
            var (nextState, output) = _fsm.TransitionFrom(CurrentState, (Context, input));
            if (CurrentState != nextState)
                Log.Information("{0} is now {1}", ModuleName, nextState);
            CurrentState = nextState;
            return output;
        }

        public bool CanHandle(DomainEvent @event)
        {
            return (CurrentState?.CanHandle(@event)).GetValueOrDefault();
        }           

        public BaseState<CONTEXT> CurrentState { get; private set; }
        public CONTEXT Context { get; }
        private readonly FSM<BaseState<CONTEXT>, (CONTEXT, DomainEvent), DomainEvent> _fsm;

        public static Runner<CONTEXT> CreateWithSingleState(CONTEXT context, BaseState<CONTEXT> singleState) =>
            new Runner<CONTEXT>(context, singleState, new[] {singleState.Define()});
        
        public static Runner<CONTEXT> Create(CONTEXT context, BaseState<CONTEXT> currentState, BaseState<CONTEXT>[] states, Transition<BaseState<CONTEXT>, (CONTEXT,DomainEvent)>[] transitions)
        {
            var defs = states.Select(s => s.Define()).ToArray();
            var initialState = states.First();
            return new Runner<CONTEXT>(context, initialState, defs,transitions)
                {
                    CurrentState = currentState
                };
        }

        
        public Runner(CONTEXT context, BaseState<CONTEXT> initialState,
            StateDefinition<BaseState<CONTEXT>, (CONTEXT, DomainEvent), DomainEvent>[] stateDefinitions,
            params Transition<BaseState<CONTEXT>, (CONTEXT, DomainEvent)>[] transitions)
        {
            CurrentState = initialState;
            Context = context;
            _fsm = new FSM<BaseState<CONTEXT>, (CONTEXT, DomainEvent), DomainEvent>(initialState, stateDefinitions, transitions);
        }
    }

    public static class RunnerExtensions
    {
        public static DomainEvent[] PlayEvents<CONTEXT>(this Runner<CONTEXT> @this, params DomainEvent[] @events)
        {
            var resultingEvents = @events.SelectMany(@this.Handle).ToArray();
            if (resultingEvents.Length > 0)
            {
                @this.PlayEvents(resultingEvents);
            }
            return resultingEvents;
        }
    }
}