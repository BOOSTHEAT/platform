using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Bus
{
    public class EventBus : IEventBus, IReceiveAppStartSignal
    {
        public static EventBus Create() => new EventBus();

        private readonly ConcurrentDictionary<Type, List<Subscription>> _subscriptions;
        private readonly ConcurrentQueue<DomainEvent> _eventQueue;
        private bool _isApplicationStarted;

        protected EventBus()
        {
            _subscriptions = new ConcurrentDictionary<Type, List<Subscription>>();
            _eventQueue = new ConcurrentQueue<DomainEvent>();
        }

        public void Subscribe<T>(object subscriber, Action<DomainEvent> action) =>
            Subscribe(subscriber, typeof(T), action);


        public void Subscribe(object subscriber, Type subscribedType, Action<DomainEvent> action)
        {
            var subscription = new Subscription(subscriber, subscribedType, action);
            _subscriptions.AddOrUpdate(subscribedType,
                new List<Subscription>() {subscription},
                (_, existingSubscriptions) => new List<Subscription>(existingSubscriptions) {subscription});
        }


        public void Publish(params DomainEvent[] domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                _eventQueue.Enqueue(domainEvent);
            }
            Dispatch();
        }

        private void Dispatch()
        {
            if (!_isApplicationStarted) return;
            while (_eventQueue.TryDequeue(out var evt))
            {
                var subscriptions = SubscriptionsFor(evt.GetType());
                foreach (var sub in subscriptions)
                {
                    var callback = sub.Callback();
                    callback(evt);
                }
            }
        }

        public void UnSubscribe<T>(object subscriber) => UnSubscribe(subscriber, typeof(T));

        public void UnSubscribe(object subscriber, Type eventType)
        {
            _subscriptions.AddOrUpdate(eventType,
                new List<Subscription>(),
                (_, existingSubscriptions) => existingSubscriptions
                    .Where(subscription => subscription.Subscriber != subscriber).ToList());
        }

        public void SignalApplicationStarted()
        {
            _isApplicationStarted = true;
            Dispatch();
        }

        private List<Subscription> SubscriptionsFor(Type eventType) =>
            _subscriptions.GetValueOrDefault(eventType, new List<Subscription>());
    }

    internal class Subscription
    {
        private readonly Action<DomainEvent> _callback;
        public object Subscriber { get; }
        public Type EventType { get; }

        public Action<DomainEvent> Callback() => _callback;

        public Subscription(object subscriber, Type eventType, Action<DomainEvent> callback)
        {
            Subscriber = subscriber;
            EventType = eventType;
            _callback = callback;
        }
    }
}