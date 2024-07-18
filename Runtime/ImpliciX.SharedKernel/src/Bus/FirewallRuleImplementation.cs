using System;
using ImpliciX.Language.Store;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Bus
{
    public class FirewallRuleImplementation
    {
        public string ModuleId { get; }
        public FirewallRule.DirectionKind Direction { get; }
        public FirewallRule.DecisionKind Decision { get; }
        public Func<DomainEvent, bool> Predicate { get; }

        public FirewallRuleImplementation(string moduleId, FirewallRule.DirectionKind direction, FirewallRule.DecisionKind decision, Func<DomainEvent, bool> predicate)
        {
            ModuleId = moduleId;
            Direction = direction;
            Decision = decision;
            Predicate = predicate;
        }
    }
    
}