using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Store;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Bus
{
    public interface IEventBusWithFirewall : IEventBus
    {
        void AddFirewallRuleSet(List<FirewallRuleImplementation> ruleSet);
        void Publish(string senderId, DomainEvent resultingEvent);
        void ResetFirewallRuleSet();
    }

    public class EventBusWithFirewall : EventBus, IEventBusWithFirewall
    {
        public static EventBusWithFirewall CreateWithFirewall() => new EventBusWithFirewall();

        public List<FirewallRuleImplementation> CurrentRuleSet { get; private set; }

        private EventBusWithFirewall()
        {
            CurrentRuleSet  = new List<FirewallRuleImplementation>();
        }

        public void Publish(string senderId, DomainEvent @event)
        {
            var resultingEvent = Filter(senderId, @event);

            if (resultingEvent.IsSome)
                Publish(resultingEvent.GetValue());
        }

        public void ResetFirewallRuleSet()
        {
            CurrentRuleSet.Clear();
        }

        private Option<DomainEvent> Filter(string sender, DomainEvent @event)
        {
            if (CurrentRuleSet.Any(rule => IsMatching(sender, @event, rule)))
            {
                return Option<DomainEvent>.None();
            }

            return @event;
        }

        private static bool IsMatching(string sender, DomainEvent resultingEvent, FirewallRuleImplementation r)
        {
            return r.Predicate(resultingEvent)
                   && r.Decision == FirewallRule.DecisionKind.Reject
                   && r.Direction == FirewallRule.DirectionKind.From
                   && sender.Equals(r.ModuleId, StringComparison.OrdinalIgnoreCase);
        }

        public void AddFirewallRuleSet(List<FirewallRuleImplementation> ruleSet)
        {
            CurrentRuleSet = ruleSet;
        }
    }
}