using System;
using ImpliciX.Control.DomainEvents;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.TestsCommon;
using NUnit.Framework;

namespace ImpliciX.Control.Tests.TestUtilities
{
    public class SetupSubSystemTests
    {
        static SetupSubSystemTests()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(SetupSubSystemTests).Assembly);
        }

        [SetUp]
        public void SetUp()
        {
            TestTime = TimeSpan.Zero;
            ExecutionEnvironment = new ExecutionEnvironment();
        }

        protected SubSystem<TState> CreateSut<TState>(TState initState, SubSystemDefinition<TState> definition) where TState : Enum
        {
            return new SubSystem<TState>(definition, ExecutionEnvironment, EventFactoryFunc, initState);
        }

        protected SubSystem<TState> CreateSut<TState>(TState initState, SubSystemDefinition<TState> definition, ExecutionEnvironment executionEnvironment)
            where TState : Enum
        {
            return new SubSystem<TState>(definition, executionEnvironment, EventFactoryFunc, initState);
        }

        protected ExecutionEnvironment ExecutionEnvironment { get; set; }
        protected TimeSpan TestTime { get; private set; }


        protected void WithProperties(params (Urn urn, object value)[] properties)
        {
            var changed0 = PropertiesChangedHelper.CreatePropertyChanged(TimeSpan.Zero, properties);
            ExecutionEnvironment.Changed(changed0);
        }

        private IDomainEventFactory EventFactoryFunc
        {
            get
            {
                var f = EventFactory.Create(new ModelFactory(this.GetType().Assembly), () => TestTime);
                return ImpliciXEventFactory.Create(f);
            }
        }
    }
}