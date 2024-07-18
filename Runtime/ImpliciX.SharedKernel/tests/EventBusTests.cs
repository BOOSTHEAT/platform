using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Tests.Doubles;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class EventBusTests
    {
        [Test]
        public void one_subscriber_should_receive_message()
        {
            var subscriber = new SpySubscriber();
            Bus.Subscribe<FooEvent>(subscriber, evt => subscriber.Receive(evt));
            Bus.Publish(new FooEvent());
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(1);
        }

        [Test]
        public void many_subscribers_for_different_messages()
        {
            Bus.SignalApplicationStarted();
            var fooSubscriber = new SpySubscriber();
            var barSubscriber = new SpySubscriber();
            Bus.Subscribe<FooEvent>(fooSubscriber, evt => fooSubscriber.Receive(evt));
            Bus.Subscribe<BarEvent>(barSubscriber, evt => barSubscriber.Receive(evt));
            Bus.Publish(new FooEvent());
            Bus.Publish(new BarEvent());

            Check.That(fooSubscriber.CountOf<FooEvent>()).IsEqualTo(1);
            Check.That(barSubscriber.CountOf<BarEvent>()).IsEqualTo(1);
        }

        [Test]
        public void many_subscribers_for_the_same_message()
        {
            var fooSubscriber = new SpySubscriber();
            var fooSubscriber2 = new SpySubscriber();
            Bus.Subscribe<FooEvent>(fooSubscriber, evt => fooSubscriber.Receive(evt));
            Bus.Subscribe<FooEvent>(fooSubscriber2, evt => fooSubscriber2.Receive(evt));
            Bus.Publish(new FooEvent());

            Check.That(fooSubscriber.CountOf<FooEvent>()).IsEqualTo(1);
            Check.That(fooSubscriber2.CountOf<FooEvent>()).IsEqualTo(1);
        }

        [Test]
        public void one_subscriber_can_subscribe_for_multiple_events()
        {
            var subscriber = new SpySubscriber();
            Bus.Subscribe<FooEvent>(subscriber, (evt) => subscriber.Receive(evt));
            Bus.Subscribe<BarEvent>(subscriber, (evt) => subscriber.Receive(evt));
            Bus.Publish(new FooEvent());
            Bus.Publish(new BarEvent());
            Bus.Publish(new BarEvent());

            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(1);
            Check.That(subscriber.CountOf<BarEvent>()).IsEqualTo(2);
        }

        [Test]
        public void subscriber_can_unsubscribe_for_events()
        {
            var subscriber = new SpySubscriber();
            Bus.Subscribe<FooEvent>(subscriber, (evt) => subscriber.Receive(evt));
            Bus.Subscribe<BarEvent>(subscriber, (evt) => subscriber.Receive(evt));
            Bus.Publish(new FooEvent());
            Bus.Publish(new BarEvent());

            Bus.UnSubscribe<BarEvent>(subscriber);

            Bus.Publish(new BarEvent());
            Bus.Publish(new FooEvent());
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(2);
            Check.That(subscriber.CountOf<BarEvent>()).IsEqualTo(1);
        }

        [Test]
        public void when_subscribed_for_sub_type_and_gets_notified_with_supertypes_messages()
        {
            var subscriber = new SpySubscriber();
            Bus.Subscribe<FooSubtypeEvent>(subscriber, (evt) => subscriber.Receive(evt));
            Bus.Publish(new FooEvent());
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(0);
        }

        [Test]
        public void when_subscribed_for_sub_type_and_gets_notified_with_subtypes_messages()
        {
            var subscriber = new SpySubscriber();
            Bus.Subscribe<FooSubtypeEvent>(subscriber, (evt) => subscriber.Receive(evt));
            Bus.Publish(new FooSubtypeEvent());
            Check.That(subscriber.CountOf<FooSubtypeEvent>()).IsEqualTo(1);
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(0);
        }

        [Test]
        public void the_published_messages_are_on_kept_until_the_signal_is_sent()
        {
            NotSignaledBus.Publish(new FooEvent());
            var subscriber = new SpySubscriber();
            NotSignaledBus.Subscribe<FooEvent>(subscriber, (evt) => subscriber.Receive(evt));
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(0);
            NotSignaledBus.SignalApplicationStarted();
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(1);
        }


        [SetUp]
        public void SetUp()
        {
            Bus = EventBus.Create();
            Bus.SignalApplicationStarted();
            NotSignaledBus = EventBus.Create();
        }

        public EventBus NotSignaledBus { get; private set; }

        public EventBus Bus { get; private set; }
    }

    public class FooSubtypeEvent : FooEvent
    {
    }
}