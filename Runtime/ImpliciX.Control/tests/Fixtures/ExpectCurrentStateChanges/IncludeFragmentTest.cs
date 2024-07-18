using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectCurrentStateChanges
{
    [TestFixture]
    public class IncludeFragmentTest : SetupSubSystemTests
    {
        [Test]
        public void simple_fragment_include()
        {
            var sut = CreateSut(IncludeFragment.State.A, new IncludeFragment());
            var activationRequest = EventCommandRequested(examples.include_fragment._activate, default, TestTime);
            sut.PlayEvents(activationRequest);
            Check.That(sut.CurrentState).IsEqualTo(IncludeFragment.State.Aif);
        }

        [Test]
        public void fragment_with_composite_should_go_to_the_correct_state()
        {
            var sut = CreateSut(IncludeFragmentWithComposite.State.A, new IncludeFragmentWithComposite());
            sut.PlayEvents(EventCommandRequested(examples.include_fragment._activate, default, TestTime));
            Check.That(sut.CurrentState).IsEqualTo(IncludeFragmentWithComposite.State.B);
            sut.PlayEvents(EventCommandRequested(examples.include_fragment._toBc, default, TestTime));
            Check.That(sut.CurrentState).IsEqualTo(IncludeFragmentWithComposite.State.D);
        }
    }
}