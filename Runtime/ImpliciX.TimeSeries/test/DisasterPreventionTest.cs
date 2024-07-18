using System;
using System.Collections.Generic;
using InfluxDB.Client.Writes;
using NUnit.Framework;

namespace ImpliciX.TimeSeries.Test
{
  [TestFixture]
  public class DisasterPreventionTest
  {
    [Test]
    public void should_count_write_errors()
    {
      using var influxDbAdapter = new FakeInfluxDb();
      using var dp = new DisasterPrevention(5, influxDbAdapter);
      Assert.That(dp.ErrorCount, Is.EqualTo(0));
      dp.WritePoints(new PointData[] {});
      Assert.That(dp.ErrorCount, Is.EqualTo(0));

      influxDbAdapter.RaiseErrorOnWrite = true;
      dp.WritePoints(new PointData[] {});
      Assert.That(dp.ErrorCount, Is.EqualTo(1));
      dp.WritePoints(new PointData[] {});
      Assert.That(dp.ErrorCount, Is.EqualTo(2));
    }
    
    [Test]
    public void should_stop_writing_if_too_many_errors()
    {
      using var influxDbAdapter = new FakeInfluxDb();
      influxDbAdapter.RaiseErrorOnWrite = true;
      using var dp = new DisasterPrevention(3, influxDbAdapter);
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(1));
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(2));
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(3));
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(4));
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(4));
    }
    
    [Test]
    public void should_try_writing_sometimes_after_stopping_due_to_errors()
    {
      using var influxDbAdapter = new FakeInfluxDb();
      influxDbAdapter.RaiseErrorOnWrite = true;
      using var dp = new DisasterPrevention(3, influxDbAdapter);
      dp.WritePoints(new PointData[] {});
      dp.WritePoints(new PointData[] {});
      dp.WritePoints(new PointData[] {});
      
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(4));
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(4));
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(4));
      
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(5));
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(5));
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(5));
      
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(6));
    }
    
    [Test]
    public void should_reset_error_count_when_retry_succeeds()
    {
      using var influxDbAdapter = new FakeInfluxDb();
      influxDbAdapter.RaiseErrorOnWrite = true;
      using var dp = new DisasterPrevention(3, influxDbAdapter);
      dp.WritePoints(new PointData[] {});
      dp.WritePoints(new PointData[] {});
      dp.WritePoints(new PointData[] {});
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(4));
      Assert.That(dp.ErrorCount, Is.EqualTo(4));
      
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(4));
      Assert.That(dp.ErrorCount, Is.EqualTo(5));
      
      influxDbAdapter.RaiseErrorOnWrite = false;
      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(4));
      Assert.That(dp.ErrorCount, Is.EqualTo(6));

      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(5));
      Assert.That(dp.ErrorCount, Is.EqualTo(0));

      dp.WritePoints(new PointData[] {});
      Assert.That(influxDbAdapter.WriteCount, Is.EqualTo(6));
      Assert.That(dp.ErrorCount, Is.EqualTo(0));
    }

    class FakeInfluxDb : IInfluxDbAdapter
    {
      public void Dispose()
      {
      }
      
      public bool RaiseErrorOnWrite { get; set; }
      public int WriteCount { get; private set; }

      public bool WritePoints(IEnumerable<PointData> pointData)
      {
        WriteCount++;
        if(RaiseErrorOnWrite)
          throw new NotImplementedException();
        return true;
      }
    }
  }
}

