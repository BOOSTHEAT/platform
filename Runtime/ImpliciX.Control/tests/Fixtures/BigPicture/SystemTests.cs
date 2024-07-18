using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Control.Tests.Fixtures.BigPicture
{
    [TestFixture]
    public class SystemTests
    {
        [Test]
        public void should_contain_subsystems_defined_in_the_assembly()
        {
            var system = new UserDefinedControlSystem(
                new FakeDomainFactory(), 
                ApplicationRuntimeDefinition.FindStateMachines(typeof(SystemTests).Assembly)
                );
            var fooSubsystemExists = system.SubSystems.Any(ss => ss.GetType() == typeof(SubSystem<FooStates>));
            var barSubsystemExists = system.SubSystems.Any(ss => ss.GetType() == typeof(SubSystem<BarStates>));
            Check.That(fooSubsystemExists).IsTrue();
            Check.That(barSubsystemExists).IsTrue();
        }

        [Test]
        public void should_not_contain_fragment_defined_in_assembly()
        {
            var system = new UserDefinedControlSystem(
                new FakeDomainFactory(), 
                ApplicationRuntimeDefinition.FindStateMachines(typeof(SystemTests).Assembly)
                );
            var any = system.SubSystems.Any(c => c is SubSystem<BazStates>);
            Check.That(any).IsFalse();
        }

        private class FakeDomainFactory : IDomainEventFactory
        {
            public PropertiesChanged NewEvent(Urn group, IEnumerable<IDataModelValue> modelValues)
            {
                throw new NotImplementedException();
            }

            public Result<DomainEvent> NewEventResult(Urn urn, object value)
            {
                throw new System.NotImplementedException();
            }

            public Result<DomainEvent> NewEventResult(Urn group, Urn urn, object value)
            {
                throw new NotImplementedException();
            }

            public PropertiesChanged NewEvent(IEnumerable<IDataModelValue> modelValues)
            {
                throw new System.NotImplementedException();
            }

            public Result<PropertiesChanged> NewEventResult(IEnumerable<(Urn urn, object value)> properties)
            {
                throw new System.NotImplementedException();
            }

            public Func<TimeSpan> Clock => ()=>TimeSpan.Zero;
        }
    }

    public enum FooStates
    {
        A,
    }

    public enum BarStates
    {
        A,
    }

    public enum BazStates
    {
        A,
    }

    public class Foo_SubSystemDefinition : SubSystemDefinition<FooStates>
    {
        public Foo_SubSystemDefinition()
        {
            Subsystem(new SubSystemNode("foo_sys", new RootModelNode("foo"))).Initial(FooStates.A).Define(FooStates.A);
        }
    }

    public class Bar_SubSystemDefinition : SubSystemDefinition<BarStates>
    {
        public Bar_SubSystemDefinition()
        {
            Subsystem(new SubSystemNode("bar_sys", new RootModelNode("bar"))).Initial(BarStates.A).Define(BarStates.A);
        }
    }

    public class Baz_FragmentDefinition : FragmentDefinition<BazStates>
    {
        public Baz_FragmentDefinition()
        {
            Fragment(new SubSystemNode("baz_sys", new RootModelNode("baz")), new SubSystemNode("bar_sys", new RootModelNode("bar"))).Initial(BazStates.A).Define(BazStates.A);
        }
    }
}