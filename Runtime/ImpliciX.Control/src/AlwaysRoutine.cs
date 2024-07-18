using System.Linq;
using ImpliciX.Language.Control;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Control
{
    public class AlwaysRoutine : EventFuncsBuilder
    {
        public AlwaysRoutine(OnState definition, IExecutionEnvironment executionEnvironment, IDomainEventFactory eventFactory)
            : base(executionEnvironment, eventFactory)
        {
            Setup(definition);
        }

        public DomainEvent[] Execute(PropertiesChanged propertiesChanged) =>
            OnStateFuncs.SelectMany(f => f(propertiesChanged)).ToArray();

    }
}