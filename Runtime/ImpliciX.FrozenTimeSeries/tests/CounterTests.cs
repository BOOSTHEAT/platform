namespace ImpliciX.FrozenTimeSeries.Tests;

public class CounterTests
{
  [Test]
  public void IncrementCounter()
  {
    var counter = new ColdRunner.Counter(10, TimeSpan.Zero, TimeSpan.FromSeconds(5));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(2)), Is.EqualTo((11,false)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(6)), Is.EqualTo((12,true)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(9)), Is.EqualTo((13,false)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(16)), Is.EqualTo((14,true)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(22)), Is.EqualTo((15,true)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(24)), Is.EqualTo((16,false)));
  }
  
  [Test]
  public void NoLog()
  {
    var counter = new ColdRunner.Counter(10, TimeSpan.Zero, TimeSpan.FromSeconds(0));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(2)), Is.EqualTo((11,false)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(6)), Is.EqualTo((12,false)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(9)), Is.EqualTo((13,false)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(16)), Is.EqualTo((14,false)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(22)), Is.EqualTo((15,false)));
    Assert.That(counter.Inc(TimeSpan.FromSeconds(24)), Is.EqualTo((16,false)));
  }
}