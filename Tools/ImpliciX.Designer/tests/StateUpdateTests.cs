using System;
using System.Collections.Generic;
using System.Linq;
using DynamicData;
using DynamicData.Binding;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests
{
    [TestFixture]
    public class StateUpdateTests
    {
        [Test]
        public void two_subsystems_have_their_state_updated()
        {
            var concierge = ILightConcierge.Create();
            var ss1 = new SubSystemViewModelSpy("ss1", concierge);
            var ss2 = new SubSystemViewModelSpy("ss2", concierge);
            Check.That(ss1.StateActivationHistory).ContainsExactly((nameof(S.StateDa), false), (nameof(S.StateDb), false), (nameof(S.StateA), false),
                (nameof(S.StateB), false), (nameof(S.StateC), false));
            Check.That(ss2.StateActivationHistory).ContainsExactly((nameof(S.StateDa), false), (nameof(S.StateDb), false), (nameof(S.StateA), false),
                (nameof(S.StateB), false), (nameof(S.StateC), false));
            ss1.StateActivationHistory.Clear();
            ss2.StateActivationHistory.Clear();
            concierge.Session.Properties.AddOrUpdate(new[]
            {
                new ImpliciXProperty("ss1:state", nameof(S.StateB)),
                new ImpliciXProperty("ss2:state", nameof(S.StateC))
            });
            Check.That(ss1.StateActivationHistory).ContainsExactly((nameof(S.StateB), true));
            Check.That(ss2.StateActivationHistory).ContainsExactly((nameof(S.StateC), true));
            ss1.StateActivationHistory.Clear();
            ss2.StateActivationHistory.Clear();
            concierge.Session.Properties.AddOrUpdate(new[]
            {
                new ImpliciXProperty("ss1:state", nameof(S.StateA)),
                new ImpliciXProperty("ss2:state", nameof(S.StateA))
            });
            Check.That(ss1.StateActivationHistory).ContainsExactly((nameof(S.StateB), false), (nameof(S.StateA), true));
            Check.That(ss2.StateActivationHistory).ContainsExactly((nameof(S.StateC), false), (nameof(S.StateA), true));
            ss1.StateActivationHistory.Clear();
            ss2.StateActivationHistory.Clear();
            concierge.Session.Properties.Remove(new[]
            {
                new ImpliciXProperty("ss1:state", string.Empty),
                new ImpliciXProperty("ss2:state", string.Empty)
            });
            Check.That(ss1.StateActivationHistory).ContainsExactly((nameof(S.StateA), false));
            Check.That(ss2.StateActivationHistory).ContainsExactly((nameof(S.StateA), false));
        }

        [Test]
        public void outgoing_transitions_include_transitions_from_composite_parent()
        {
            var concierge = ILightConcierge.Create();
            var ss1 = new SubSystemViewModelSpy("ss1", concierge);
            var ss2 = new SubSystemViewModelSpy("ss2", concierge);

            var lsot = ss1.ViewModel.LeafStatesAndOutgoingTransitions;
            Check.That(lsot.Count).IsEqualTo(5);
            var sbn = lsot.Keys.ToDictionary(x => x.Name, x => x);
            IEnumerable<string> OutgoingTransitions(string state) =>
                lsot[sbn[state]].Extracting(t => t.Description).OrderBy(s => s);
            Check.That(OutgoingTransitions(nameof(S.StateA))).ContainsExactly("gotoB()", "gotoC()", "gotoD()");
            Check.That(OutgoingTransitions(nameof(S.StateB))).ContainsExactly("gotoA()", "gotoC()");
            Check.That(OutgoingTransitions(nameof(S.StateC))).ContainsExactly("gotoB()");
            Check.That(OutgoingTransitions(nameof(S.StateDa))).ContainsExactly("gotoB()", "gotoC()", "gotoDb()");
            Check.That(OutgoingTransitions(nameof(S.StateDb))).ContainsExactly("gotoB()", "gotoC()", "gotoDa()");
        }

        [Test]
        public void transitions_are_activated_including_composites()
        {
            var concierge = ILightConcierge.Create();
            var ss1 = new SubSystemViewModelSpy("ss1", concierge);
            var ss2 = new SubSystemViewModelSpy("ss2", concierge);

            Check.That(ss1.TransitionActivationHistory).ContainsExactly(
                ("StateDa:gotoDb()", false),
                ("StateDa:gotoB()", false),
                ("StateDa:gotoC()", false),
                ("StateDb:gotoDa()", false),
                ("StateDb:gotoB()", false),
                ("StateDb:gotoC()", false),
                ("StateA:gotoB()", false),
                ("StateA:gotoC()", false),
                ("StateA:gotoD()", false),
                ("StateB:gotoC()", false),
                ("StateB:gotoA()", false),
                ("StateC:gotoB()", false)
            );
            ss1.TransitionActivationHistory.Clear();
            concierge.Session.Properties.AddOrUpdate(new[]
            {
                new ImpliciXProperty("ss1:state", nameof(S.StateB))
            });
            Check.That(ss1.TransitionActivationHistory).ContainsExactly(
                ("StateB:gotoC()", true),
                ("StateB:gotoA()", true)
            );
            ss1.TransitionActivationHistory.Clear();
            concierge.Session.Properties.AddOrUpdate(new[]
            {
                new ImpliciXProperty("ss1:state", nameof(S.StateDa))
            });
            Check.That(ss1.TransitionActivationHistory).ContainsExactly(
                ("StateB:gotoC()", false),
                ("StateB:gotoA()", false),
                ("StateDa:gotoDb()", true),
                ("StateDa:gotoB()", true),
                ("StateDb:gotoB()", true),
                ("StateDa:gotoC()", true),
                ("StateDb:gotoC()", true)
            );
        }

        class SubSystemViewModelSpy
        {
            public SubSystemViewModelSpy(string name, ILightConcierge concierge)
            {
                ViewModel = new SubSystemViewModel(name, concierge, (addTransition, _, __) => FakeSubSystem.Create(addTransition));
                foreach (var svm in ViewModel.LeafStatesAndOutgoingTransitions.Keys)
                    svm.WhenPropertyChanged(x => x.IsActive).Subscribe(x => StateActivationHistory.Add((svm.Name, x.Value)));
                var transitions = (
                        from x in ViewModel.LeafStatesAndOutgoingTransitions
                        from transition in x.Value
                        let description = $"{x.Key.Name}:{transition.Description}"
                        select (description, transition)
                    ).Distinct().ToArray();
                foreach (var t in transitions)
                    t.transition.WhenPropertyChanged(x => x.IsActive)
                        .Subscribe(x =>
                            TransitionActivationHistory.Add((t.description, x.Value)));
            }

            public readonly SubSystemViewModel ViewModel;
            public readonly List<(string, bool)> StateActivationHistory = new List<(string, bool)>();
            public readonly List<(string, bool)> TransitionActivationHistory = new List<(string, bool)>();
        }

        public enum S
        {
            StateA,
            StateB,
            StateC,
            StateD,
            StateDa,
            StateDb
        }

        class FakeSubSystem : SubSystemDefinition<S>
        {
            public static void Create(Action<BaseStateViewModel, BaseStateViewModel, DefinitionViewModel> addTransition)
            {
                var ss = new FakeSubSystem();
                ViewModelBuilder.Run(ss, addTransition, _ => { }, (b, fragment) => { });
            }

            private FakeSubSystem()
            {
        // @formatter:off
        Subsystem(root.fakeSubsystem)
        .Initial(S.StateA)
          .Define(S.StateA)
            .Transitions
              .WhenMessage(gotoB).Then(S.StateB)
              .WhenMessage(gotoC).Then(S.StateC)
              .WhenMessage(gotoD).Then(S.StateD)
          .Define(S.StateB)
            .Transitions
              .WhenMessage(gotoC).Then(S.StateC)
              .WhenMessage(gotoA).Then(S.StateA)
          .Define(S.StateC)
            .Transitions
              .WhenMessage(gotoB).Then(S.StateB)
          .Define(S.StateD)
            .Transitions
              .WhenMessage(gotoB).Then(S.StateB)
              .WhenMessage(gotoC).Then(S.StateC)
          .Define(S.StateDa).AsInitialSubStateOf(S.StateD)
            .Transitions
              .WhenMessage(gotoDb).Then(S.StateDb)
          .Define(S.StateDb).AsSubStateOf(S.StateD)
            .Transitions
              .WhenMessage(gotoDa).Then(S.StateDa);
                // @formatter:on
            }
            CommandUrn<NoArg> gotoA => CommandUrn<NoArg>.Build(nameof(gotoA));
            CommandUrn<NoArg> gotoB => CommandUrn<NoArg>.Build(nameof(gotoB));
            CommandUrn<NoArg> gotoC => CommandUrn<NoArg>.Build(nameof(gotoC));
            CommandUrn<NoArg> gotoD => CommandUrn<NoArg>.Build(nameof(gotoD));
            CommandUrn<NoArg> gotoDa => CommandUrn<NoArg>.Build(nameof(gotoDa));
            CommandUrn<NoArg> gotoDb => CommandUrn<NoArg>.Build(nameof(gotoDb));
        }
    }
}