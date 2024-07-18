using System;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.SharedKernel.Bus
{
    public interface IEventBus
    {
        void Subscribe<T>(object subscriber, Action<DomainEvent> action);
        void Publish(params DomainEvent[] evt);
        void UnSubscribe<T>(object subscriber);
        void Subscribe(object subscriber, Type subscribedType, Action<DomainEvent> action);
        void UnSubscribe(object subscriber, Type eventType);
    }
}