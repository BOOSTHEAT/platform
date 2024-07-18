using System;
using System.Linq;
using ImpliciX.Data;
using ImpliciX.Language;
using ImpliciX.RuntimeFoundations.Factory;
using ImpliciX.SystemSoftware.States;

namespace ImpliciX.SystemSoftware
{
    public class SystemSoftwareRunner
    {
        public static Runner<Context> Create(
            SystemSoftwareModuleDefinition moduleDefinition,
            Context context,
            Loader loader,
            IDomainEventFactory domainEventFactory,
            Type currentStateType = null)
        {
            var enabled = new Enabled(moduleDefinition, domainEventFactory);
            var starting = new Starting(moduleDefinition, domainEventFactory) { ParentState = enabled, IsInitial = true };
            var ready = new Ready(moduleDefinition, domainEventFactory) { ParentState = enabled };
            var updating = new Updating(moduleDefinition, loader, domainEventFactory) { ParentState = enabled };
            var commiting = new Commiting(moduleDefinition, domainEventFactory) { ParentState = enabled };

            var states = new BaseState<Context>[] { starting, enabled, ready, updating, commiting };
            var currentState = states.FirstOrDefault(state => state.GetType() == currentStateType);

            var transitions = new[]
            {
                starting.WhenSoftwareVersionsGathered(ready),
                ready.WhenGeneralUpdateCommandReceived(updating),
                updating.WhenUpdateCompletedForAllDevices(commiting),
                updating.WhenUpdateCanceled(ready)
            };

            return Runner<Context>.Create(context, currentState, states, transitions);
        }
    }
}