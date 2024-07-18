using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ImpliciX.Designer.Simulation;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.Simulation
{
    [TestFixture]
    public class ScenarioTest
    {
        [Test]
        public void successfully_load_a_scenario()
        {
            var data = @"
00:00:00,command,general:labmode:start,virtual
00:00:10,properties,service:dhw:top_temperature:measure,1200
,,service:dhw:top_temperature:status,Success
,,service:dhw:bottom_temperature:measure,300
,,service:dhw:bottom_temperature:status,Success
00:00:20,command,production:main_circuit:SWITCH,InternalFlow
00:00:40,properties,service:heating:differential_temperature_setpoint,20
,,service:heating:thermostat:demand,On
00:01:00,command,general:labmode:stop,
";
            var memoryStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(data)));
            var result = Scenario.Create(memoryStream);
            var expected = new List<MessageEvent>
            {
                new MessageEvent(TimeSpan.Zero, "command").Add("general:labmode:start", "virtual"),
                new MessageEvent(TimeSpan.FromSeconds(10), "properties")
                    .Add("service:dhw:top_temperature:measure", "1200")
                    .Add("service:dhw:top_temperature:status", "Success")
                    .Add("service:dhw:bottom_temperature:measure", "300")
                    .Add("service:dhw:bottom_temperature:status", "Success"),
                new MessageEvent(TimeSpan.FromSeconds(20), "command")
                    .Add("production:main_circuit:SWITCH", "InternalFlow"),
                new MessageEvent(TimeSpan.FromSeconds(40), "properties")
                    .Add("service:heating:differential_temperature_setpoint", "20")
                    .Add("service:heating:thermostat:demand", "On"),
                new MessageEvent(TimeSpan.FromSeconds(60), "command").Add("general:labmode:stop", "")
            };
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Value.Events).IsEqualTo(expected);
        }

        [Test]
        public void failure_bad_timespan()
        {
            var data = @"
00:06:66,command,general:labmode:start,virtual
00:00:10,properties,service:dhw:top_temperature:measure,1200
";
            var memoryStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(data)));
            var result = Scenario.Create(memoryStream);
            Check.That(result.IsError).IsTrue();
        }

        [Test]
        public void failure_bad_properties_sequence()
        {
            var data = @"
,,service:dhw:top_temperature:status,Success
,,service:dhw:bottom_temperature:measure,300
,,service:dhw:bottom_temperature:status,Success
00:00:20,command,production:main_circuit:SWITCH,InternalFlow
00:00:40,properties,service:heating:differential_temperature_setpoint,20
,,service:heating:thermostat:demand,On
00:01:00,command,general:labmode:stop,
";
            var memoryStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(data)));
            var result = Scenario.Create(memoryStream);
            Check.That(result.IsError).IsTrue();
        }
        [Test]

        public void failure_bad_command_sequence()
        {
            var data = @"
00:00:00,command,general:labmode:start,virtual
,,production:main_circuit:SWITCH,InternalFlow
00:00:40,properties,service:heating:differential_temperature_setpoint,20
,,service:heating:thermostat:demand,On
00:01:00,command,general:labmode:stop,
";
            var memoryStream = new StreamReader(new MemoryStream(Encoding.UTF8.GetBytes(data)));
            var result = Scenario.Create(memoryStream);
            Check.That(result.IsError).IsTrue();
        }
    }
}