using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NUnit.Framework;

namespace ImpliciX.Driver.Dumb.Tests;

public class DiscreteTests
{
  
  [Test]
  public void Bug7078_Discrete()
  {
    var sim = new PropertiesSimulation();
    var urn = PropertyUrn<States>.Build("root", "foo");
    var inputValues = sim.Discrete(urn, States.Running, (States.Running, 0.4), (States.Disabled, 0.3), (States.Failure, 0.3));
    var tsAtStart = TimeSpan.FromMilliseconds(int.MaxValue);
    var times = Enumerable.Range(0, 10).Select(i => tsAtStart + TimeSpan.FromSeconds(i));
    var events = times.Select(time => PropertiesChanged.Create(inputValues(time), time)).ToArray();
    var allValues = events.SelectMany(e => e.ModelValues.Select(o => o.ModelValue())).ToArray();
    var states = allValues.Distinct().Order().ToArray();
    Assert.That(states, Is.EqualTo(new[] {States.Running, States.Disabled, States.Failure}));
  }

  private enum States
  {
    Running = 100,
    Disabled,
    Failure
  }
}