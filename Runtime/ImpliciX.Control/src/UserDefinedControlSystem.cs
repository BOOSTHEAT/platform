using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Control.DomainEvents;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.RuntimeFoundations.Factory;

namespace ImpliciX.Control
{
    public class UserDefinedControlSystem : ControlSystem
    {
        public UserDefinedControlSystem(IDomainEventFactory domainEventFactory, params ISubSystemDefinition[] definitions)
        {
            var localEventFactory = ImpliciXEventFactory.Create(domainEventFactory);
            SubSystems = CreateSubSystems(ExecutionEnvironment, localEventFactory, definitions).ToList();
        }

        private static IEnumerable<IImpliciXSystem> CreateSubSystems(IExecutionEnvironment executionEnvironment, IDomainEventFactory domainEventFactory,
            IEnumerable<ISubSystemDefinition> definitions)
        {
            return definitions.Select(def => CreateSubsystem(def, executionEnvironment, domainEventFactory));
        }

        private static IImpliciXSystem CreateSubsystem(ISubSystemDefinition definition, IExecutionEnvironment executionEnvironment,
            IDomainEventFactory domainEventFactory)
        {
            var stateType = FindBaseType(definition.GetType(), t => t.GenericTypeArguments.Length == 1).GenericTypeArguments;
            var subSystemType = typeof(SubSystem<>).MakeGenericType(stateType);
            var optionType = typeof(Option<>).MakeGenericType(stateType);
            var none = Activator.CreateInstance(optionType, true);
            return (IImpliciXSystem) Activator.CreateInstance(subSystemType, definition, executionEnvironment, domainEventFactory, none);
        }

        private static Type FindBaseType(Type t, Predicate<Type> condition) => (t == null || condition(t))
            ? t
            : FindBaseType(t.BaseType, condition);
    }
}