using System;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RuntimeFoundations.Tests
{
    [TestFixture]
    public class SystemTickedTests
    {
        [TestCase((ushort)1000,(uint)1,(uint) 60,false)]
        [TestCase((ushort)1000,(uint)59, (uint) 60,false)]
        [TestCase((ushort)1000,(uint)60, (uint) 60,true)]
        [TestCase((ushort)100,(uint)60, (uint) 60,false)]
        [TestCase((ushort)100,(uint)600, (uint) 60,true)]
        public void system_ticked_period_elapsed(ushort basePeriod, uint tickCount, uint interval, bool expected)
        {
            var sut = SystemTicked.Create(basePeriod, tickCount);
            Check.That(sut.IsPeriodElapsed(TimeSpan.FromSeconds(interval))).IsEqualTo(expected);
        }
        
        [TestCase((ushort)1000,(uint)1,false)]
        [TestCase((ushort)1000,(uint)59,false)]
        [TestCase((ushort)1000,(uint)60,true)]
        [TestCase((ushort)100,(uint)60,false)]
        [TestCase((ushort)100,(uint)600,true)]
        public void system_ticked_minute_elapsed(ushort basePeriod, uint tickCount, bool expected)
        {
            var sut = SystemTicked.Create(basePeriod, tickCount);
            Check.That(sut.IsMinuteElapsed()).IsEqualTo(expected);
        }
        
        [TestCase((ushort)1000,(uint)1,false)]
        [TestCase((ushort)1000,(uint)59,false)]
        [TestCase((ushort)1000,(uint)3600,true)]
        [TestCase((ushort)1000,(uint)3701,false)]
        [TestCase((ushort)1000,(uint)7200,true)]
        [TestCase((ushort)100,(uint)3600,false)]
        [TestCase((ushort)100,(uint)36000,true)]
        [TestCase((ushort)100,(uint)72000,true)]
        public void system_ticked_hour_elapsed(ushort basePeriod, uint tickCount, bool expected)
        {
            var sut = SystemTicked.Create(basePeriod, tickCount);
            Check.That(sut.IsHourElapsed()).IsEqualTo(expected);
        }

        [TestCase("2021/03/07 14:59:17.999", (ushort) 1000, "2021/03/07 14:59:59.999", (uint) 42, 1, false)]
        [TestCase("2021/03/07 14:59:18.000", (ushort) 1000, "2021/03/07 15:00:00.000", (uint) 42, 1, true)]
        [TestCase("2021/03/07 14:59:18.001", (ushort) 1000, "2021/03/07 15:00:00.001", (uint) 42, 1, true)]
        [TestCase("2021/03/07 14:59:18.999", (ushort) 1000, "2021/03/07 15:00:00.999", (uint) 42, 1, true)]
        
        [TestCase("2021/03/07 14:59:17", (ushort) 1000, "2021/03/07 14:59:59", (uint) 42, 1, false)]
        [TestCase("2021/03/07 14:59:18", (ushort) 1000, "2021/03/07 15:00:00", (uint) 42, 1, true)]
        [TestCase("2021/03/07 14:59:19", (ushort) 1000, "2021/03/07 15:00:01", (uint) 42, 1, false)]
        
        [TestCase("2021/03/07 14:59:17", (ushort) 100, "2021/03/07 14:59:59", (uint) 420, 1, false)]
        [TestCase("2021/03/07 14:59:18", (ushort) 100, "2021/03/07 15:00:00", (uint) 420, 1, true)]
        [TestCase("2021/03/07 14:59:19", (ushort) 100, "2021/03/07 15:00:01", (uint) 420, 1, false)]

        [TestCase("2021/03/07 14:59:17.999", (ushort) 100, "2021/03/07 14:59:59.999", (uint) 420, 1, false)]
        [TestCase("2021/03/07 14:59:18.000", (ushort) 100, "2021/03/07 15:00:00.000", (uint) 420, 1, true)]
        [TestCase("2021/03/07 14:59:18.001", (ushort) 100, "2021/03/07 15:00:00.001", (uint) 420, 1, true)]
        [TestCase("2021/03/07 14:59:18.101", (ushort) 100, "2021/03/07 15:00:00.101", (uint) 420, 1, false)]

        [TestCase("2021/03/07 14:59:17.999", (ushort) 10, "2021/03/07 14:59:59.999", (uint) 4200, 1, false)]
        [TestCase("2021/03/07 14:59:18.000", (ushort) 10, "2021/03/07 15:00:00.000", (uint) 4200, 1, true)]
        [TestCase("2021/03/07 14:59:18.001", (ushort) 10, "2021/03/07 15:00:00.001", (uint) 4200, 1, true)]
        [TestCase("2021/03/07 14:59:18.011", (ushort) 10, "2021/03/07 15:00:00.011", (uint) 4200, 1, false)]

        [TestCase("2021/03/07 14:59:17", (ushort) 1000, "2021/03/07 15:00:59", (uint) 102, 1, false)]
        [TestCase("2021/03/07 14:59:18", (ushort) 1000, "2021/03/07 15:01:00", (uint) 102, 1, true)]
        [TestCase("2021/03/07 14:59:19", (ushort) 1000, "2021/03/07 15:01:01", (uint) 102, 1, false)]
        
        [TestCase("2021/03/07 14:59:17", (ushort) 1000, "2021/03/07 14:59:59", (uint) 42, 60, false)]
        [TestCase("2021/03/07 14:59:18", (ushort) 1000, "2021/03/07 15:00:00", (uint) 42, 60, true)]
        [TestCase("2021/03/07 14:59:19", (ushort) 1000, "2021/03/07 15:00:01", (uint) 42, 60, false)]
        
        [TestCase("2021/03/07 13:59:17", (ushort) 1000, "2021/03/07 14:59:59", (uint) 3642, 60, false)]
        [TestCase("2021/03/07 13:59:18", (ushort) 1000, "2021/03/07 15:00:00", (uint) 3642, 60, true)]
        [TestCase("2021/03/07 13:59:19", (ushort) 1000, "2021/03/07 15:00:01", (uint) 3642, 60, false)]
        
        [TestCase("2021/03/07 14:14:17", (ushort) 1000, "2021/03/07 14:59:59", (uint) 2742, 45, false)]
        [TestCase("2021/03/07 14:14:18", (ushort) 1000, "2021/03/07 15:00:00", (uint) 2742, 45, true)]
        [TestCase("2021/03/07 14:14:19", (ushort) 1000, "2021/03/07 15:00:01", (uint) 2742, 45, false)]
        
        [TestCase("2021/03/07 13:29:17", (ushort) 1000, "2021/03/07 14:59:59", (uint) 5442, 45, false)]
        [TestCase("2021/03/07 13:29:18", (ushort) 1000, "2021/03/07 15:00:00", (uint) 5442, 45, true)]
        [TestCase("2021/03/07 13:29:19", (ushort) 1000, "2021/03/07 15:00:01", (uint) 5442, 45, false)]
        
        [TestCase("2021/03/07 13:59:17", (ushort) 100, "2021/03/07 14:59:59", (uint) 36420, 60, false)]
        [TestCase("2021/03/07 13:59:18", (ushort) 100, "2021/03/07 15:00:00", (uint) 36420, 60, true)]
        [TestCase("2021/03/07 13:59:19", (ushort) 100, "2021/03/07 15:00:01", (uint) 36420, 60, false)]

        [TestCase("2021/03/07 13:59:17", (ushort) 100, "2021/03/07 23:59:59", (uint) 360420, 1440, false)]
        [TestCase("2021/03/07 13:59:18", (ushort) 100, "2021/03/08 00:00:00", (uint) 360420, 1440, true)]
        [TestCase("2021/03/07 13:59:19", (ushort) 100, "2021/03/08 00:00:01", (uint) 360420, 1440, false)]
        public void system_ticked_is_next_date(string startDate, ushort basePeriod, string currentDate, uint tickCount, int period, bool expected)
        {
            var at = new TimeSpan(DateTime.Parse(currentDate).Ticks);
            var origin = new TimeSpan(DateTime.Parse(startDate).Ticks);
            var sut = SystemTicked.Create(origin, basePeriod, tickCount);
            var span = TimeSpan.FromMinutes(period);
            Check.That(origin + TimeSpan.FromMilliseconds(sut.BasePeriod * sut.TickCount)).Equals(at);
            Check.That(sut.IsNextDate(span)).IsEqualTo(expected);
        }

        [Test]
        public void system_ticked_shall_last_human_lifespan()
        {
            var origin = new TimeSpan(DateTime.Parse("2023/01/01 00:00:00").Ticks);
            var basePeriodInMilliseconds = (ushort)1000;
            var ticks = uint.MaxValue;
            var ticked = SystemTicked.Create(origin, basePeriodInMilliseconds, ticks);
            var spanForUintMaxTick = new TimeSpan(DateTime.Parse("2159/02/07 06:28:15").Ticks);
            Check.That(ticked.At).IsEqualTo(spanForUintMaxTick);
        }
    }
}