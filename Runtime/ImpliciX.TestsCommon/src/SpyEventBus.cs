using System;
using System.Collections.Generic;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;

namespace ImpliciX.TestsCommon
{
    public class SpyEventBus : IEventBus, IEventBusWithFirewall
    {
        public List<Type> RecordedUnSubscriptions { get; }
        public List<Type> RecordedSubscriptions { get; }
        public List<DomainEvent> RecordedPublications { get; }


        public SpyEventBus()
        {
            RecordedSubscriptions = new List<Type>();
            RecordedUnSubscriptions = new List<Type>();
            RecordedPublications = new List<DomainEvent>();
        }

        public void Subscribe<T>(object subscriber, Action<DomainEvent> action)
        {
            Subscribe(subscriber, typeof(T), action);
        }

        public void Publish(params DomainEvent[] evt)
        {
            RecordedPublications.AddRange(evt);
        }

        public void UnSubscribe<T>(object subscriber)
        {
            UnSubscribe(subscriber, typeof(T));
        }

        public void Subscribe(object subscriber, Type subscribedType, Action<DomainEvent> action)
        {
            RecordedSubscriptions.Add(subscribedType);
        }

        public void UnSubscribe(object subscriber, Type eventType)
        {
            RecordedUnSubscriptions.Add(eventType);
        }

        public void AddFirewallRuleSet(List<FirewallRuleImplementation> ruleSet)
        {
            throw new NotImplementedException();
        }

        public void Publish(string senderId, DomainEvent resultingEvent)
        {
            RecordedPublications.Add(resultingEvent);
        }

        public void ResetFirewallRuleSet()
        {
            throw new NotImplementedException();
        }
    }
}