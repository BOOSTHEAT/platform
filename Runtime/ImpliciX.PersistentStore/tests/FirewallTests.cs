using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.Language.Store;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Bus;
using ImpliciX.SharedKernel.Tests.Doubles;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.PersistentStore.Tests
{
  public class FirewallTests
  {
    private RootModelNode root;
    private CommandNode<Literal> start;
    private CommandNode<NoArg> stop;
    private CommandNode<Literal> rejectable;
    private ModelFactory modelFactory;
    private EventBusWithFirewall bus;
    private DomainEventHandler<CommandRequested> handler;
    private Func<CommandRequested, bool> predicate;
    private SpySubscriber subscriber;
    
    [SetUp]
    public void Init()
    {
      root = new RootModelNode(nameof(root));
      start = CommandNode<Literal>.Create(nameof(start),root);
      stop = CommandNode<NoArg>.Create(nameof(stop), root);
      rejectable = CommandNode<Literal>.Create(nameof(rejectable),root);
      modelFactory = new ModelFactory(typeof(FirewallTests).Assembly);
      bus = EventBusWithFirewall.CreateWithFirewall();
      subscriber = new SpySubscriber();
      bus.SignalApplicationStarted();
      bus.Subscribe<FooEvent>(subscriber, subscriber.Receive);
      bus.Subscribe<CommandRequested>(subscriber, subscriber.Receive);
      (handler, predicate) = PersistentStoreModule.CreateFirewallHandler(bus, start.command, stop.command,
        new Dictionary<string, IEnumerable<FirewallRule>>()
        {
          {
            "foo", new[]
            {
              FirewallRule.RejectAll().From("fizz"),
              FirewallRule.RejectAll().From("buzz"),
            }
          },
          {
            "bar", new[]
            {
              FirewallRule.Reject(rejectable).From("plop"),
            }
          },
        }
      );
    }

    [TestCase]
    public void predicate_selection()
    {
      Check.That(predicate(CommandRequested.Create(start.command, Literal.Create("x"), TimeSpan.Zero))).IsTrue();
      Check.That(predicate(CommandRequested.Create(stop.command, new NoArg(), TimeSpan.Zero))).IsTrue();
      Check.That(predicate(CommandRequested.Create(rejectable.command, Literal.Create("x"), TimeSpan.Zero))).IsFalse();
    }
    
    [TestCase]
    public void reject_any_event_from_some_modules()
    {
      handler(CommandRequested.Create(start.command, Literal.Create("foo"), TimeSpan.Zero));
      bus.Publish("fizz", new FooEvent {Foo = 1});
      bus.Publish("buzz", new FooEvent {Foo = 2});
      bus.Publish("gotcha", new FooEvent {Foo = 3});
      Check.That(subscriber.ReceivedEventsOf<FooEvent>().Select(e => e.Foo)).ContainsExactly( new []
      {
        3
      });
    }
    
    [TestCase]
    public void reject_commands_from_some_modules()
    {
      handler(CommandRequested.Create(start.command, Literal.Create("bar"), TimeSpan.Zero));
      bus.Publish("plop", CommandRequested.Create(rejectable.command, Literal.Create("no"), TimeSpan.Zero));
      bus.Publish("gotcha", CommandRequested.Create(rejectable.command, Literal.Create("yes"), TimeSpan.Zero));
      Check.That(subscriber.ReceivedEventsOf<CommandRequested>().Select(e => e.Arg)).ContainsExactly( new []
      {
        Literal.Create("yes")
      });
    }
    
        
    [TestCase]
    public void stop_rejecting_any_event()
    {
      handler(CommandRequested.Create(start.command, Literal.Create("foo"), TimeSpan.Zero));
      handler(CommandRequested.Create(stop.command, new NoArg(), TimeSpan.Zero));
      bus.Publish("fizz", new FooEvent {Foo = 1});
      bus.Publish("buzz", new FooEvent {Foo = 2});
      bus.Publish("gotcha", new FooEvent {Foo = 3});
      Check.That(subscriber.ReceivedEventsOf<FooEvent>().Select(e => e.Foo)).ContainsExactly( new []
      {
        1,2,3
      });
    }

  }
}