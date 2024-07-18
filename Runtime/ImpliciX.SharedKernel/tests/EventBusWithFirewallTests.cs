using System.Collections.Generic;
using ImpliciX.Language.Store;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Tests.Doubles;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class EventBusWithFirewallTests
    {
        [SetUp]
        public void Init()
        {
            Sut = EventBusWithFirewall.CreateWithFirewall();
            Sut.SignalApplicationStarted();
        }

        private EventBusWithFirewall Sut { get; set; }

        [Test]
        public void every_messages_are_authorized_by_default()
        {
            var subscriber = new SpySubscriber();
            Sut.Subscribe<FooEvent>(subscriber, subscriber.Receive);
            Sut.Publish("testID", new FooEvent());
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(1);
        }

        [Test]
        public void reject_messages_from_specific_sender()
        {
            var subscriber = new SpySubscriber();
            Sut.Subscribe<FooEvent>(subscriber, subscriber.Receive);
            Sut.Subscribe<BarEvent>(subscriber, subscriber.Receive);
            var ruleSet = new List<FirewallRuleImplementation>
                {new FirewallRuleImplementation("fooID", FirewallRule.DirectionKind.From, FirewallRule.DecisionKind.Reject, @event => true)};
            Sut.AddFirewallRuleSet(ruleSet);
            Sut.Publish("fooID", new FooEvent {Foo = 2});
            Sut.Publish("barID", new BarEvent());
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(0);
            Check.That(subscriber.CountOf<BarEvent>()).IsEqualTo(1);
        }

        [Test]
        public void reset_every_rules()
        {
            var subscriber = new SpySubscriber();
            Sut.Subscribe<FooEvent>(subscriber, subscriber.Receive);
            var ruleSet = new List<FirewallRuleImplementation>
                {new FirewallRuleImplementation("fooID", FirewallRule.DirectionKind.From, FirewallRule.DecisionKind.Reject, @event => true)};
            Sut.AddFirewallRuleSet(ruleSet);
            Sut.ResetFirewallRuleSet();
            Sut.Publish("fooID", new FooEvent {Foo = 2});
            Check.That(subscriber.CountOf<FooEvent>()).IsEqualTo(1);
        }
    }
}