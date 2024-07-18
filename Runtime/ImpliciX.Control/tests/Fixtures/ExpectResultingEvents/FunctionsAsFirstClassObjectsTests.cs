using System;
using ImpliciX.Control.Tests.Examples;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.TestUtilities;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;
using static ImpliciX.TestsCommon.EventsHelper;

namespace ImpliciX.Control.Tests.Fixtures.ExpectResultingEvents
{
    [TestFixture]
    public class FunctionsAsFirstClassObjectsTests:SetupSubSystemTests
    {
        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(fun_example).Assembly);
        }

        [Test]
        // [Ignore("wip")]
        public void should_choose_function()
        {
            var sut = new UserDefinedControlSystem(DomainEventFactory(TimeSpan.Zero), new FunctionSelector());
            
            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero, 
                    (fun_example.settings.fa, new FunctionDefinition(new (string Name, float Value)[] {("a0", 0f), ("a1", 2f)})),
                    (fun_example.settings.fb, new FunctionDefinition(new (string Name, float Value)[] {("a0", 0f), ("a1", 3f)})),
                    (fun_example.settings.fun_choice, FunChoice.A)))
            ).ContainsExactly(
                EventPropertyChanged(TimeSpan.Zero,
                    (fun_example.selector.selected_fun, new FunctionDefinition(new[]{("a0",0f),("a1",2f)})))
                );

            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero,
                    (fun_example.selector.x, 0.1f)))
            ).ContainsExactly(
            EventPropertyChanged(TimeSpan.Zero,
                (fun_example.selector.z, 0.1f),
                (fun_example.selector.y, 0.2f))
            );
            
            Check.That(sut.PlayEvents(
                EventPropertyChanged(TimeSpan.Zero,
                    (fun_example.settings.fun_choice, FunChoice.B)))
            ).ContainsExactly(
                EventPropertyChanged(TimeSpan.Zero,
                    (fun_example.selector.selected_fun, new FunctionDefinition(new[]{("a0",0f),("a1",3f)}))),
                EventPropertyChanged(TimeSpan.Zero,
                    (fun_example.selector.y, 0.3f))
            );
        }
    }
}