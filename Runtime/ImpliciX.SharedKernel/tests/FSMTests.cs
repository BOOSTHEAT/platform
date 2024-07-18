using System;
using ImpliciX.SharedKernel.FiniteStateMachine;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class FSMTests : SetupFsmTests
    {
        [Test]
        public void transition_simplest_case()
        {
            var transitions = new[] {CreateTransition(MyState.A, MyState.B)};
            var stateA = CreateStateDefinition(MyState.A);
            var stateB = CreateStateDefinition(MyState.B);
            var fsm = CreateFSM(MyState.A, new[] {stateA, stateB}, transitions);
            var transitionOutcome = fsm.TransitionFrom(MyState.A, 0);
            Check.That(transitionOutcome.nextState).IsEqualTo(MyState.B);
        }

        [Test]
        public void states_are_not_necessary_enums()
        {
            var fsm = new FSM<string, int, int>(
                "A",
                new[] {new StateDefinition<string, int, int>("A"), new StateDefinition<string, int, int>("B")},
                new[] {new Transition<string, int>("A", "B")});
            var transitionOutcome = fsm.TransitionFrom("A", 0);
            Check.That(transitionOutcome.nextState).IsEqualTo("B");
        }

        [Test]
        public void transition_on_condition()
        {
            var transitions = new[]
            {
                CreateTransition(MyState.A, MyState.B, (x) => x >= 2),
                CreateTransition(MyState.A, MyState.C, (x) => x < 2)
            };

            var stateA = CreateStateDefinition(MyState.A);
            var stateB = CreateStateDefinition(MyState.B);
            var stateC = CreateStateDefinition(MyState.C);

            var fsm = CreateFSM(MyState.A, new[] {stateA, stateB, stateC}, transitions);
            var (newState, outputs) = fsm.TransitionFrom(MyState.A, 0);
            Check.That(newState).IsEqualTo(MyState.C);
        }


        [Test]
        public void execute_functions_on_entry()
        {
            var transitions = new[]
            {
                CreateTransition(MyState.A, MyState.B, (x) => x >= 2),
            };

            var stateA = CreateStateDefinition(MyState.A);
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[]
            {
                x => (x + 1).AsArray(),
                x => (x + 2).AsArray(),
            });

            var fsm = CreateFSM(MyState.A, new[] {stateA, stateB}, transitions);
            var transitionOutcome = fsm.TransitionFrom(MyState.A, 2);
            Check.That(transitionOutcome.ouput).ContainsExactly(new[] {3, 4});
        }

        [Test]
        public void execute_functions_on_entry_for_initial_state()
        {
            var stateA = CreateStateDefinition(MyState.A, new Func<int, int[]>[]
            {
                x => (x + 1).AsArray(),
                x => (x + 2).AsArray(),
            });

            var fsm = CreateFSM(MyState.A, new[] {stateA}, new Transition<MyState, int>[0]);
            var transitionOutcome = fsm.Activate(2);
            Check.That(transitionOutcome.output).ContainsExactly(new[] {3, 4});
        }

        [Test]
        public void execute_functions_on_exit()
        {
            var transitions = new[]
            {
                CreateTransition(MyState.A, MyState.B),
                CreateTransition(MyState.B, MyState.C),
            };
            var stateA = CreateStateDefinition(MyState.A);
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[] { }, new Func<int, int[]>[]
            {
                x => (x + 1).AsArray(),
                x => (x + 2).AsArray(),
            });
            var stateC = CreateStateDefinition(MyState.C);
            var fsm = CreateFSM(MyState.A, new[] {stateA, stateB, stateC}, transitions);
            var transitionOutcome = fsm.TransitionFrom(MyState.B, 0);
            Check.That(transitionOutcome.ouput).ContainsExactly(new[] {1, 2});
        }

        [Test]
        public void transition_when_no_condition_is_satisfied()
        {
            var stateA = CreateStateDefinition(MyState.A);
            var stateB = CreateStateDefinition(MyState.B,
                new Func<int, int[]>[] {x => (x++).AsArray()}
            );
            var transitions = new[]
            {
                CreateTransition(MyState.A, MyState.B, (x) => x > 4),
            };
            var fsm = CreateFSM(MyState.A, new[] {stateA, stateB}, transitions);
            var transitionOutcome = fsm.TransitionFrom(MyState.A, 1);
            Check.That(transitionOutcome.nextState).IsEqualTo(MyState.A);
            Check.That(transitionOutcome.ouput).IsEmpty();
        }

        [Test]
        public void execute_functions_on_state()
        {
            var stateA = CreateStateDefinition(MyState.A, 
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] { }, 
                new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[] {x => (x++).AsArray()});
            var transitions = new[]
            {
                CreateTransition(MyState.A, MyState.B, (x) => x > 4),
            };
            var fsm = CreateFSM(MyState.A, new[] {stateA, stateB}, transitions);
            var transitionOutcome = fsm.TransitionFrom(MyState.A, 0);
            Check.That(transitionOutcome.nextState).IsEqualTo(MyState.A);
            Check.That(transitionOutcome.ouput).ContainsExactly(10);
        }
    }
}