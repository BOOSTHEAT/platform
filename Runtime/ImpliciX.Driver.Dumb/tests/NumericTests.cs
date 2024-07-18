using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NUnit.Framework;

namespace ImpliciX.Driver.Dumb.Tests;

[TestFixture(typeof(Power))]
[TestFixture(typeof(Energy))]
[TestFixture(typeof(Volume))]
[TestFixture(typeof(AngularSpeed))]
[TestFixture(typeof(Current))]
[TestFixture(typeof(DifferentialPressure))]
[TestFixture(typeof(DifferentialTemperature))]
[TestFixture(typeof(Flow))]
[TestFixture(typeof(Percentage))]
[TestFixture(typeof(Pressure))]
[TestFixture(typeof(RotationalSpeed))]
[TestFixture(typeof(StandardLiterPerMinute))]
[TestFixture(typeof(Temperature))]
[TestFixture(typeof(Voltage))]
public class NumericTests<T> where T : IFloat<T>
{
  [TestCase(0, 100)]
  [TestCase(0, 1)]
  public void TestSinusoid(double min, double max)
  {
    var events = CreateSinusoidEvents(min, max, 1.0);
    Assert.That(events, Has.All.Matches(Has.Property("ModelValues").Exactly(2).Items));
    var eventValues = events.Select(pc =>
      ((MeasureStatus) pc.ModelValues.First().ModelValue(), ((IFloat<T>)pc.ModelValues.Last().ModelValue()).ToFloat()));
    foreach (var (status, value) in eventValues)
    {
      Assert.That(status, Is.EqualTo(MeasureStatus.Success));
      Assert.That(value, Is.GreaterThan(min));
      Assert.That(value, Is.LessThan(max));
    }
  }

  [TestCase(0, 100, 0.8, 0.6, 10, 90)]
  [TestCase(0, 100, 0.9, 0.8, 5, 95)]
  [TestCase(100, 200, 0.9, 0.8, 105, 195)]
  public void TestSinusoidThreshold(double min, double max, double failThreshold,
    double expectedPercentageOfSuccess, double expectedLowerBound, double expectedUpperBound)
  {
    Assert.That(failThreshold, Is.GreaterThan(0));
    Assert.That(failThreshold, Is.LessThan(1));
    var events = CreateSinusoidEvents(min, max, failThreshold);
    var eventValues = events
      .Where(pc => (MeasureStatus) pc.ModelValues.First().ModelValue() == MeasureStatus.Success)
      .Select(pc =>((IFloat<T>)pc.ModelValues.Last().ModelValue()).ToFloat())
      .ToArray();

    var percentageOfFailure = 2 * (1 - failThreshold);
    var percentageOfSuccess = 1 - percentageOfFailure;
    AssertEqualFloat(percentageOfSuccess, expectedPercentageOfSuccess);
    Assert.That(eventValues.Count(), Is.InRange(1000*(percentageOfSuccess-0.1), 1000*percentageOfSuccess), "Success count for 1000 samples");
    
    var shift = (max - min) * percentageOfFailure / 4;
    Assert.That(shift, Is.GreaterThan(0));

    var lowerBound = min + shift;
    AssertEqualFloat(lowerBound, expectedLowerBound);
    Assert.That(eventValues.Min(), Is.InRange(lowerBound, lowerBound+shift));

    var upperBound = max - shift;
    AssertEqualFloat(upperBound, expectedUpperBound);
    Assert.That(eventValues.Max(), Is.InRange(upperBound-shift, upperBound));
  }

  private static void AssertEqualFloat(double actual, double expected)
  {
    Assert.That(actual, Is.InRange(expected * 0.9999, expected * 1.0001));
  }

  private static PropertiesChanged[] CreateSinusoidEvents(double min, double max, double failThreshold)
  {
    var sim = new PropertiesSimulation();
    var node = new MeasureNode<T>(Urn.BuildUrn("foo"), new RootModelNode("root"));
    var sin = sim.Sinusoid(node, min, max, failThreshold);
    var times = Enumerable.Range(0, 1000).Select(i => TimeSpan.FromSeconds(i));
    var events = times.Select(time => PropertiesChanged.Create(sin(time), time)).ToArray();
    return events;
  }

  [Test]
  public void TestStepper()
  {
    var sim = new PropertiesSimulation();
    var urn = PropertyUrn<T>.Build("root", "foo");
    var sin = sim.Stepper(urn, 0, TimeSpan.FromSeconds(1), 400);
    var times = Enumerable.Range(0, 1000).Select(i => TimeSpan.FromSeconds(i));
    var events = times.Select(time => PropertiesChanged.Create(sin(time), time)).ToArray();
    Assert.That(events, Has.All.Matches(Has.Property("ModelValues").Exactly(1).Items));
  }

}