using System;
using System.Collections.Generic;
using ImpliciX.Data.Metrics;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Language.Model;
using ImpliciX.TimeMath.Computers.StateMonitoring;
using ImpliciX.TimeMath.Tests.Helpers;
using NUnit.Framework;

namespace ImpliciX.TimeMath.Tests.StateMonitoring;

public class StateDigestTests
{
  [Test]
  public void DigestAggregatesStateChanges()
  {
    var digest = StateDigest.Neutral + ScFoo314 + ScBar511;
    
    Assert.That(digest[X.Foo].Occurrence, Is.EqualTo(3));
    Assert.That(digest[X.Foo].Duration, Is.EqualTo(TimeSpan.FromSeconds(14)));
    Assert.That(digest[X.Bar].Occurrence, Is.EqualTo(5));
    Assert.That(digest[X.Bar].Duration, Is.EqualTo(TimeSpan.FromSeconds(11)));
    Assert.That(digest[X.Qix].Occurrence, Is.EqualTo(0));
    Assert.That(digest[X.Qix].Duration, Is.EqualTo(TimeSpan.Zero));
  }
  
  [Test]
  public void AddOccurrenceAndDurationOnSameState()
  {
    var digest = (StateDigest.Neutral + ScFoo314) + ScFoo511;
    
    Assert.That(digest[X.Foo].Occurrence, Is.EqualTo(8));
    Assert.That(digest[X.Foo].Duration, Is.EqualTo(TimeSpan.FromSeconds(25)));
  }
  
  [Test]
  public void CanAddStateChangeOnTheLeft()
  {
    var digest = ScFoo314 + (ScFoo511 + StateDigest.Neutral);
    
    Assert.That(digest[X.Foo].Occurrence, Is.EqualTo(8));
    Assert.That(digest[X.Foo].Duration, Is.EqualTo(TimeSpan.FromSeconds(25)));
  }
  
  [Test]
  public void HasStartingAndEndingChanges()
  {
    var digest = StateDigest.Neutral + ScFoo314;
    
    Assert.True(digest.StartsWith(X.Foo), "StartsWith");
    Assert.True(digest.EndsWith(X.Foo), "EndsWith");
  }
  
  [Test]
  public void HasStartingAndEndingChangesUpdates()
  {
    var digest = StateDigest.Neutral + ScFoo314 + ScBar511;
    
    Assert.True(digest.StartsWith(X.Foo), "StartsWith");
    Assert.False(digest.StartsWith(X.Bar), "StartsWith");
    Assert.False(digest.StartsWith(X.Qix), "StartsWith");
    Assert.False(digest.EndsWith(X.Foo), "EndsWith");
    Assert.True(digest.EndsWith(X.Bar), "EndsWith");
    Assert.False(digest.EndsWith(X.Qix), "EndsWith");
  }
  
  [Test]
  public void HasStartingAndEndingChangesOnTheLeft()
  {
    var digest = ScFoo314 + StateDigest.Neutral;
    
    Assert.True(digest.StartsWith(X.Foo), "StartsWith");
    Assert.True(digest.EndsWith(X.Foo), "EndsWith");
  }
  
  [Test]
  public void HasStartingAndEndingChangesUpdatesOnTheLeft()
  {
    var digest = ScFoo314 + (ScBar511 + StateDigest.Neutral);
    
    Assert.True(digest.StartsWith(X.Foo), "StartsWith");
    Assert.False(digest.StartsWith(X.Bar), "StartsWith");
    Assert.False(digest.EndsWith(X.Foo), "EndsWith");
    Assert.True(digest.EndsWith(X.Bar), "EndsWith");
  }
  
  [Test]
  public void AddDigestToNeutral()
  {
    var digest = StateDigest.Neutral + ScFoo314 + ScBar511;
    
    Assert.That(digest + StateDigest.Neutral, Is.EqualTo(digest));
    Assert.That(StateDigest.Neutral + digest, Is.EqualTo(digest));
  }
  
  [Test]
  public void AddDigestsWithCompatibleEndStartSequence()
  {
    var digest1 = StateDigest.Neutral + ScFoo314 + ScBar511;
    var digest2 = StateDigest.Neutral + ScBar511 + ScFoo314;
    var result1 = digest1 + digest2;
    var result2 = digest2 + digest1;
    
    Assert.That(result1[X.Foo].Occurrence, Is.EqualTo(6));
    Assert.That(result1[X.Foo].Duration, Is.EqualTo(TimeSpan.FromSeconds(28)));
    Assert.That(result1[X.Bar].Occurrence, Is.EqualTo(9));
    Assert.That(result1[X.Bar].Duration, Is.EqualTo(TimeSpan.FromSeconds(22)));
    Assert.That(result1[X.Qix].Occurrence, Is.EqualTo(0));
    Assert.That(result1[X.Qix].Duration, Is.EqualTo(TimeSpan.Zero));
    Assert.True(result1.StartsWith(X.Foo), "StartsWith");
    Assert.False(result1.StartsWith(X.Bar), "StartsWith");
    Assert.True(result1.EndsWith(X.Foo), "EndsWith");
    Assert.False(result1.EndsWith(X.Bar), "EndsWith");
    
    Assert.That(result2[X.Foo].Occurrence, Is.EqualTo(5));
    Assert.That(result2[X.Foo].Duration, Is.EqualTo(TimeSpan.FromSeconds(28)));
    Assert.That(result2[X.Bar].Occurrence, Is.EqualTo(10));
    Assert.That(result2[X.Bar].Duration, Is.EqualTo(TimeSpan.FromSeconds(22)));
    Assert.That(result2[X.Qix].Occurrence, Is.EqualTo(0));
    Assert.That(result2[X.Qix].Duration, Is.EqualTo(TimeSpan.Zero));
    Assert.False(result2.StartsWith(X.Foo), "StartsWith");
    Assert.True(result2.StartsWith(X.Bar), "StartsWith");
    Assert.False(result2.EndsWith(X.Foo), "EndsWith");
    Assert.True(result2.EndsWith(X.Bar), "EndsWith");
  }
  
  [Test]
  public void AddDigestsWithIncompatibleEndStartSequence()
  {
    var digest = StateDigest.Neutral + ScFoo314 + ScBar511;
    Assert.Throws<ArgumentException>(() =>
    {
      var _ = digest + digest;
    });
  }
  
    
  [Test]
  public void AddDigestsWithHole()
  {
    var digest1 = StateDigest.Neutral + ScFoo314 + ScBar511;
    var digest2 = StateDigest.Neutral + ScBar511 + ScFoo314;
    var result1 = digest1 + StateDigest.Hole + digest2;
    var result2 = digest2 + StateDigest.Hole + digest1;
    var result3 = digest1 + StateDigest.Hole + digest1;
    
    Assert.That(result1[X.Foo].Occurrence, Is.EqualTo(6));
    Assert.That(result1[X.Foo].Duration, Is.EqualTo(TimeSpan.FromSeconds(28)));
    Assert.That(result1[X.Bar].Occurrence, Is.EqualTo(10));
    Assert.That(result1[X.Bar].Duration, Is.EqualTo(TimeSpan.FromSeconds(22)));
    Assert.That(result1[X.Qix].Occurrence, Is.EqualTo(0));
    Assert.That(result1[X.Qix].Duration, Is.EqualTo(TimeSpan.Zero));
    Assert.True(result1.StartsWith(X.Foo), "StartsWith");
    Assert.False(result1.StartsWith(X.Bar), "StartsWith");
    Assert.True(result1.EndsWith(X.Foo), "EndsWith");
    Assert.False(result1.EndsWith(X.Bar), "EndsWith");
    
    Assert.That(result2[X.Foo].Occurrence, Is.EqualTo(6));
    Assert.That(result2[X.Foo].Duration, Is.EqualTo(TimeSpan.FromSeconds(28)));
    Assert.That(result2[X.Bar].Occurrence, Is.EqualTo(10));
    Assert.That(result2[X.Bar].Duration, Is.EqualTo(TimeSpan.FromSeconds(22)));
    Assert.That(result2[X.Qix].Occurrence, Is.EqualTo(0));
    Assert.That(result2[X.Qix].Duration, Is.EqualTo(TimeSpan.Zero));
    Assert.False(result2.StartsWith(X.Foo), "StartsWith");
    Assert.True(result2.StartsWith(X.Bar), "StartsWith");
    Assert.False(result2.EndsWith(X.Foo), "EndsWith");
    Assert.True(result2.EndsWith(X.Bar), "EndsWith");
    
    Assert.That(result3[X.Foo].Occurrence, Is.EqualTo(6));
    Assert.That(result3[X.Foo].Duration, Is.EqualTo(TimeSpan.FromSeconds(28)));
    Assert.That(result3[X.Bar].Occurrence, Is.EqualTo(10));
    Assert.That(result3[X.Bar].Duration, Is.EqualTo(TimeSpan.FromSeconds(22)));
    Assert.That(result3[X.Qix].Occurrence, Is.EqualTo(0));
    Assert.That(result3[X.Qix].Duration, Is.EqualTo(TimeSpan.Zero));
    Assert.True(result3.StartsWith(X.Foo), "StartsWith");
    Assert.False(result3.StartsWith(X.Bar), "StartsWith");
    Assert.False(result3.EndsWith(X.Foo), "EndsWith");
    Assert.True(result3.EndsWith(X.Bar), "EndsWith");
  }
  
  [Test]
  public void SubtractNeutralFromDigest()
  {
    var digest = StateDigest.Neutral + ScFoo314 + ScBar511;
    
    Assert.That(digest - StateDigest.Neutral, Is.EqualTo(digest));
  }
  
  [Test]
  public void CannotSubtractFromNeutral()
  {
    var digest = StateDigest.Neutral + ScFoo314 + ScBar511;
    
    Assert.Throws<ArgumentException>(() =>
    {
      var _ = StateDigest.Neutral - digest;
    });
  }
  
  [Test]
  public void CannotSubtractToNeutral()
  {
    var digest = StateDigest.Neutral + ScFoo314 + ScBar511;
    
    Assert.Throws<ArgumentException>(() =>
    {
      var _ = digest - digest;
    });
  }
  
  [Test]
  public void SubtractDigestsWithCompatibleStartsSequences()
  {
    var digestFB = StateDigest.Neutral + ScFoo314 + ScBar511;
    var digestF = StateDigest.Neutral + ScFoo314;
    var digestB = StateDigest.Neutral + ScBar511;
    var digestFFB = digestF + digestFB;
    var digestFFFB = digestF + digestFFB;
    
    Assert.That(digestFFB - digestF, Is.EqualTo(digestFB));
    Assert.That(digestFFFB - digestF - digestF, Is.EqualTo(digestFB));
    Assert.That(digestFB - digestF, Is.EqualTo(StateDigest.Hole + digestB));
    Assert.That(digestFFB - digestF - digestF, Is.EqualTo(StateDigest.Hole + digestB));
    Assert.That((StateDigest.Hole + digestFFB) - (StateDigest.Hole + digestF), Is.EqualTo(digestFB));
    Assert.That((digestF + StateDigest.Hole) - (digestF + StateDigest.Hole), Is.EqualTo(StateDigest.Hole));
  }
  
  [Test]
  public void CannotSubtractFromHole()
  {
    var digest = StateDigest.Neutral + ScFoo314 + ScBar511;
    
    Assert.Throws<ArgumentException>(() =>
    {
      var _ = StateDigest.Hole - digest;
    });
  }
  
  [Test]
  public void CannotSubtractFromSmallerDigest()
  {
    var digestFB = StateDigest.Neutral + ScFoo314 + ScBar511;
    var digestF = StateDigest.Neutral + ScFoo314;
    
    Assert.Throws<ArgumentException>(() =>
    {
      var _ = digestF - digestFB;
    });
  }
  
  [Test]
  public void CannotSubtractDigestsWithIncompatibleStartsSequences()
  {
    var digestFB = StateDigest.Neutral + ScFoo314 + ScBar511;
    var digestF = StateDigest.Neutral + ScFoo314;
    var digestB = StateDigest.Neutral + ScBar511;
    
    Assert.Throws<ArgumentException>(() =>
    {
      var _ = digestFB - digestB;
    });
    
    Assert.Throws<ArgumentException>(() =>
    {
      var _ = digestFB - StateDigest.Hole;
    });
    
    Assert.Throws<ArgumentException>(() =>
    {
      var _ = digestFB - (StateDigest.Hole + digestF);
    });
  }

  enum X
  {
    Foo,
    Bar,
    Qix
  }

  private static readonly StateChange ScFoo314 = new (X.Foo, 3, TimeSpan.FromSeconds(14));
  private static readonly StateChange ScFoo511 = new (X.Foo, 5, TimeSpan.FromSeconds(11));
  private static readonly StateChange ScBar511 = new (X.Bar, 5, TimeSpan.FromSeconds(11));
  
  private static StateMonitoringInfo _info = MetricInfoFactory.CreateStateMonitoringInfo(
    Metrics.Metric(MetricUrn.Build("any"))
      .Is.Every(30).Seconds.StateMonitoringOf(ExpressionsFactory.CreateUrn<X>("trigger"))
      .Builder.Build<Metric<MetricUrn>>(),
    new Dictionary<Urn, Type>()
  );

}