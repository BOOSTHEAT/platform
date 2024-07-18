using System;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class FSMCompositeTests : SetupFsmTests
    {
        [Test]
        public void composite_on_entry_enter_in_initial_state()
        {
            var stateA = CreateStateDefinition(MyState.A);
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[] {x => (x + 1).AsArray()});
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true,
                new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var stateBB = CreateStateDefinition(MyState.BB, MyState.B, false,
                new Func<int, int[]>[] {x => (x + 100).AsArray()});

            var transitions = new[]
            {
                CreateTransition(MyState.A, MyState.B),
                CreateTransition(MyState.BA, MyState.BB),
            };

            var fsm = CreateFSM(MyState.A, new[] {stateA, stateB, stateBA, stateBB}, transitions);
            var (nextState, outputs) = fsm.TransitionFrom(MyState.A, 1);
            Check.That(nextState).IsEqualTo(MyState.BA);
            Check.That(outputs).ContainsExactly(2, 11);
            (nextState, outputs) = fsm.TransitionFrom(MyState.BA, 1);
            Check.That(nextState).IsEqualTo(MyState.BB);
            Check.That(outputs).ContainsExactly(101);
        }

        [Test]
        public void composite_on_exit()
        {
            var stateB =
                CreateStateDefinition(MyState.B, new Func<int, int[]>[] { },
                    new Func<int, int[]>[] {x => (x + 1000).AsArray()});
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true, new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var stateBB = CreateStateDefinition(MyState.BB, MyState.B, false, new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 100).AsArray()});
            var stateC = CreateStateDefinition(MyState.C);

            var transitions = new[]
            {
                CreateTransition(MyState.BA, MyState.BB),
                CreateTransition(MyState.BB, MyState.C),
            };

            var fsm = CreateFSM(MyState.B, new[] {stateB, stateBA, stateBB, stateC}, transitions);

            var (nextState, outputs) = fsm.TransitionFrom(MyState.BA, 1);
            Check.That(outputs).ContainsExactly(11);

            (nextState, outputs) = fsm.TransitionFrom(MyState.BB, 1);
            Check.That(nextState).IsEqualTo(MyState.C);
            Check.That(outputs).ContainsExactly(101, 1001);
        }


        [Test]
        public void composite_state_is_the_initial_state()
        {
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[] {x => (x + 1).AsArray()});
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true,
                new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var fsm = CreateFSM(MyState.B, new[] {stateB, stateBA});
            var (nextState, outputs) = fsm.Activate(1);
            Check.That(nextState).IsEqualTo(MyState.BA);
            Check.That(outputs).ContainsExactly(2, 11);
        }

        [TestCase(MyState.BAA, 1, MyState.BB)]
        [TestCase(MyState.BAA, 4, MyState.C)]
        [TestCase(MyState.BA, -1, MyState.BB)]
        public void composite_inherited_transitions(MyState fromState, int input, MyState expectedNextState)
        {
            var stateB = CreateStateDefinition(MyState.B);
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true);
            var stateBAA = CreateStateDefinition(MyState.BAA, MyState.BA, true);
            var stateBB = CreateStateDefinition(MyState.BB, MyState.B, false);
            var stateC = CreateStateDefinition(MyState.C);

            var transitions = new[]
            {
                CreateTransition(MyState.B, MyState.C, x => x == 4),
                CreateTransition(MyState.BA, MyState.BB, x => x >= 1),
                CreateTransition(MyState.BA, MyState.BB, x => x == -1)
            };

            var fsm = CreateFSM(MyState.B, new[] {stateB, stateBA, stateBB, stateC, stateBAA}, transitions);
            var (nextState, output) = fsm.TransitionFrom(fromState, input);
            Check.That(nextState).Equals(expectedNextState);
        }

        [Test]
        public void composite_on_entry_functions_are_sorted_on_transitionned()
        {
            var stateA = CreateStateDefinition(MyState.A);
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[] {x => (x + 1).AsArray()});
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true,
                new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var stateBAA = CreateStateDefinition(MyState.BAA, MyState.BA, true,
                new Func<int, int[]>[] {x => (x + 100).AsArray()});

            var transitions = new[]
            {
                CreateTransition(MyState.A, MyState.B)
            };

            var fsm = CreateFSM(MyState.A, new[] {stateB, stateBA, stateBAA, stateA}, transitions);
            var (state, output) = fsm.TransitionFrom(MyState.A, 0);

            Check.That(output).ContainsExactly(new[] {1, 10, 100});
        }

        [Test]
        public void composite_on_entry_functions_are_sorted_on_activation()
        {
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[] {x => (x + 1).AsArray()});
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true,
                new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var stateBAA = CreateStateDefinition(MyState.BAA, MyState.BA, true,
                new Func<int, int[]>[] {x => (x + 100).AsArray()});

            var fsm = CreateFSM(MyState.B, new[] {stateB, stateBA, stateBAA});
            var (state, output) = fsm.Activate(0);

            Check.That(output).ContainsExactly(new[] {1, 10, 100});
        }

        [TestCase(MyState.BAA, 0, new[] {1000, 10, 1})]
        [TestCase(MyState.BAA, 1, new[] {1001, 11})]
        [TestCase(MyState.BA, 2, new[] {12})]
        public void composite_on_exit_functions_are_sorted_on_transitionned(MyState fromState, int x,
            int[] expectedOutput)
        {
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 1).AsArray()});
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true,
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var stateBAA = CreateStateDefinition(MyState.BAA, MyState.BA, true,
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 1000).AsArray()});
            var stateBB = CreateStateDefinition(MyState.BB, MyState.B, false,
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 100).AsArray()});
            var stateC = CreateStateDefinition(MyState.C);

            var transitions = new[]
            {
                CreateTransition(MyState.BAA, MyState.C, x => x == 0),
                CreateTransition(MyState.BAA, MyState.BB, x => x == 1),
                CreateTransition(MyState.BA, MyState.BB, x => x == 2)
            };

            var fsm = CreateFSM(MyState.A, new[] {stateB, stateBA, stateBAA, stateC, stateBB}, transitions);
            var (state, output) = fsm.TransitionFrom(fromState, x);

            Check.That(output).ContainsExactly(expectedOutput);
        }

        [Test]
        public void composite_direct_transition_to_substate()
        {
            var stateB = CreateStateDefinition(MyState.B, new Func<int, int[]>[] {x => (x + 1).AsArray()});
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true,
                new Func<int, int[]>[] {x => (x + 10).AsArray()},
                new Func<int, int[]>[] { });
            var stateBB = CreateStateDefinition(MyState.BB, MyState.B, false,
                new Func<int, int[]>[] {x => (x + 20).AsArray()},
                new Func<int, int[]>[] { });
            var stateC = CreateStateDefinition(MyState.C, new Func<int, int[]>[] {x => (x + 100).AsArray()},
                new Func<int, int[]>[] {x => (x - 100).AsArray()});
            var transitions = new[]
            {
                CreateTransition(MyState.C, MyState.BB, x => x == 0),
            };
            var fsm = CreateFSM(MyState.A, new[] {stateB, stateBA, stateBB, stateC}, transitions);
            var (state, output) = fsm.TransitionFrom(MyState.C, 0);
            Check.That(state).IsEqualTo(MyState.BB);
            Check.That(output).ContainsExactly(-100, 1, 20);
        }

        [Test]
        public void composite_onstate_substate_is_executed()
        {
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true,
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 1).AsArray()});
            var stateB = CreateStateDefinition(MyState.B);

            var fsm = CreateFSM(MyState.B, new[] {stateB, stateBA});
            var (_, output) = fsm.TransitionFrom(MyState.BA, 0);

            Check.That(output).ContainsExactly(1);
        }

        [Test]
        public void composite_onstate_parent_state_is_executed()
        {
            var stateBAA = CreateStateDefinition(MyState.BAA, MyState.BA, true, new Func<int, int[]>[] { },
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 100).AsArray()});
            var stateBA = CreateStateDefinition(MyState.BA, MyState.B, true, new Func<int, int[]>[] { },
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var stateB = CreateStateDefinition(MyState.B,
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] { },
                new Func<int, int[]>[] {x => (x + 1).AsArray()});

            var fsm = CreateFSM(MyState.B, new[] {stateB, stateBA, stateBAA});
            var (_, output) = fsm.TransitionFrom(MyState.BAA, 0);

            Check.That(output).ContainsExactly(1, 10, 100);
        }

        [Test]
        public void composite_nested_direct_transition_select_the_initial_state()
        {
            var sA = CreateStateDefinition(MyState.A);
            var sB = CreateStateDefinition(MyState.B);
            var sBA = CreateStateDefinition(MyState.BA, MyState.B, true);
            var sBB = CreateStateDefinition(MyState.BB, MyState.B, false);
            var sBC = CreateStateDefinition(MyState.BC, MyState.B, false);
            var sBCA = CreateStateDefinition(MyState.BCA, MyState.BC, true);

            var transitions = new[]
            {
                CreateTransition(MyState.A, MyState.B),
                CreateTransition(MyState.BA, MyState.BB),
                CreateTransition(MyState.BB, MyState.BC),
            };

            var fsm = CreateFSM(MyState.A, new[] {sA, sB, sBA, sBB, sBC, sBCA}, transitions);

            var (state, _) = fsm.TransitionFrom(MyState.A, 0);
            Check.That(state).Equals(MyState.BA);
        }
        
        [Test]
        public void on_entry_works_when_states_is_in_different_composite_with_different_depth()
        {
            var sA = CreateStateDefinition(MyState.A);
            var sB = CreateStateDefinition(MyState.B,MyState.A,true);
            var sBA = CreateStateDefinition(MyState.BA, MyState.B, true);
            var sD = CreateStateDefinition(MyState.D,MyState.A,false, new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var sDC = CreateStateDefinition(MyState.DC, MyState.D, true);
            var sDCA = CreateStateDefinition(MyState.DCA, MyState.DC, true);
            var sDCAA = CreateStateDefinition(MyState.DCAA, MyState.DCA, true);
            var sDCB = CreateStateDefinition(MyState.DCB, MyState.DC, false);

            var transitions = new[]
            {
                CreateTransition(MyState.BA, MyState.DCAA),
            };

            var fsm = CreateFSM(MyState.A, new[] {sA,sB, sBA, sD, sDC, sDCA, sDCAA, sDCB}, transitions);
            var (_, output) = fsm.TransitionFrom(MyState.BA, 0);
            Check.That(output).ContainsExactly(10);
        }
        
          
        [Test]
        public void on_entry_works_when_states_is_in_different_composite_with_different_depth_with_root_as_first_ancestor()
        {
            var sB = CreateStateDefinition(MyState.B);
            var sBA = CreateStateDefinition(MyState.BA, MyState.B, true);
            var sD = CreateStateDefinition(MyState.D, new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var sDC = CreateStateDefinition(MyState.DC, MyState.D, true);
            var sDCA = CreateStateDefinition(MyState.DCA, MyState.DC, true);
            var sDCAA = CreateStateDefinition(MyState.DCAA, MyState.DCA, true);
            var sDCB = CreateStateDefinition(MyState.DCB, MyState.DC, false);

            var transitions = new[]
            {
                CreateTransition(MyState.BA, MyState.DCAA),
            };

            var fsm = CreateFSM(MyState.A, new[] {sB, sBA, sD, sDC, sDCA, sDCAA, sDCB}, transitions);
            var (_, output) = fsm.TransitionFrom(MyState.BA, 0);
            Check.That(output).ContainsExactly(10);
        }
        
                [Test]
        public void on_exit_works_when_states_is_in_different_composite_with_different_depth()
        {
            var sA = CreateStateDefinition(MyState.A);
            var sB = CreateStateDefinition(MyState.B,MyState.A,true);
            var sBA = CreateStateDefinition(MyState.BA, MyState.B, true);
            var sD = CreateStateDefinition(MyState.D,MyState.A,false, new Func<int, int[]>[0],new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var sDC = CreateStateDefinition(MyState.DC, MyState.D, true);
            var sDCA = CreateStateDefinition(MyState.DCA, MyState.DC, true);
            var sDCAA = CreateStateDefinition(MyState.DCAA, MyState.DCA, true);
            var sDCB = CreateStateDefinition(MyState.DCB, MyState.DC, false);

            var transitions = new[]
            {
                CreateTransition(MyState.DCAA, MyState.BA),
            };

            var fsm = CreateFSM(MyState.A, new[] {sA,sB, sBA, sD, sDC, sDCA, sDCAA, sDCB}, transitions);
            var (_, output) = fsm.TransitionFrom(MyState.DCAA, 0);
            Check.That(output).ContainsExactly(10);
        }

        [Test]
        public void on_exit_works_when_states_is_in_different_composite_with_different_depth_with_root_as_first_ancestor()
        {
            var sB = CreateStateDefinition(MyState.B);
            var sBA = CreateStateDefinition(MyState.BA, MyState.B, true);
            var sD = CreateStateDefinition(MyState.D, new Func<int, int[]>[0],new Func<int, int[]>[] {x => (x + 10).AsArray()});
            var sDC = CreateStateDefinition(MyState.DC, MyState.D, true);
            var sDCA = CreateStateDefinition(MyState.DCA, MyState.DC, true);
            var sDCAA = CreateStateDefinition(MyState.DCAA, MyState.DCA, true);
            var sDCB = CreateStateDefinition(MyState.DCB, MyState.DC, false);

            var transitions = new[]
            {
                CreateTransition(MyState.DCAA, MyState.BA),
            };

            var fsm = CreateFSM(MyState.A, new[] {sB, sBA, sD, sDC, sDCA, sDCAA, sDCB}, transitions);
            var (_, output) = fsm.TransitionFrom(MyState.DCAA, 0);
            Check.That(output).ContainsExactly(10);
        }
    }
}    