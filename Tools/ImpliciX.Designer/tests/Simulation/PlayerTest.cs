using System;
using System.Collections.Generic;
using System.Text.Json;
using ImpliciX.Designer.Simulation;
using Microsoft.Reactive.Testing;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.Simulation
{
    [TestFixture]
    public class PlayerTest
    {
        [Test]
        public void successfully_play_a_scenario()
        {
            var scenarioEvents = new List<MessageEvent>
            {
                new MessageEvent(TimeSpan.Zero, "command").Add("general:labmode:start", "virtual"),
                new MessageEvent(TimeSpan.FromSeconds(10), "properties")
                    .Add("service:dhw:top_temperature:measure", "1200"),
                new MessageEvent(TimeSpan.FromSeconds(20), "command")
                    .Add("production:main_circuit:SWITCH", "InternalFlow"),
                new MessageEvent(TimeSpan.FromSeconds(40), "properties")
                    .Add("service:heating:thermostat:demand", "On"),
                new MessageEvent(TimeSpan.FromSeconds(60), "command").Add("general:labmode:stop", "")
            };

            var scenario = new Scenario(scenarioEvents);
            Player player = new Player(SpyWS.SendAsync, VirtualScheduler);
            player.Play(scenario);
            VirtualScheduler.AdvanceTo(TimeSpan.FromSeconds(1).Ticks);
            CheckCommandInSendJson(SpyWS.SentMessage, "general:labmode:start", "virtual");
            VirtualScheduler.AdvanceTo(TimeSpan.FromSeconds(10).Ticks);
            CheckPropertiesInSentJson(SpyWS.SentMessage, "service:dhw:top_temperature:measure", "1200");
            VirtualScheduler.AdvanceTo(TimeSpan.FromSeconds(20).Ticks);
            CheckCommandInSendJson(SpyWS.SentMessage, "production:main_circuit:SWITCH", "InternalFlow");
            VirtualScheduler.AdvanceTo(TimeSpan.FromSeconds(40).Ticks);
            CheckConfigurationInSentJson(SpyWS.SentMessage, "service:heating:thermostat:demand", "On");
            VirtualScheduler.AdvanceTo(TimeSpan.FromSeconds(60).Ticks);
            CheckCommandInSendJson(SpyWS.SentMessage, "general:labmode:stop", "");
            Check.That(SpyWS.SentMessages).CountIs(5);
        }

        private static void CheckCommandInSendJson(JsonDocument sentJson, string expectedUrn, string expected)
        {
            Check.That(sentJson).IsNotNull();
            var command = sentJson.RootElement.GetProperty("$type").GetString();
            Check.That(command).IsEqualTo("command");
            var urn = sentJson.RootElement.GetProperty("Urn").GetString();
            Check.That(urn).IsEqualTo(expectedUrn);
            var value = sentJson.RootElement.GetProperty("Argument").GetString();
            Check.That(value).IsEqualTo(expected);
        }

        private static void CheckPropertiesInSentJson(JsonDocument sentJson, string expectedUrn, string expectedValue)
        {
            Check.That(sentJson).IsNotNull();
            var command = sentJson.RootElement.GetProperty("$type").GetString();
            Check.That(command).IsEqualTo("properties");
            var propertyValue = sentJson.RootElement.GetProperty("Properties")[0];
            var urn = propertyValue.GetProperty("Urn").GetString();
            Check.That(urn).IsEqualTo(expectedUrn);
            var value = propertyValue.GetProperty("Value").GetString();
            Check.That(value).IsEqualTo(expectedValue);
        }
        
        private static void CheckConfigurationInSentJson(JsonDocument sentJson, string expectedUrn, string expectedValue)
        {
            Check.That(sentJson).IsNotNull();
            var command = sentJson.RootElement.GetProperty("$type").GetString();
            Check.That(command).IsEqualTo("properties");
            var propertyValue = sentJson.RootElement.GetProperty("Properties")[0];
            var urn = propertyValue.GetProperty("Urn").GetString();
            Check.That(urn).IsEqualTo(expectedUrn);
            var value = propertyValue.GetProperty("Value").GetString();
            Check.That(value).IsEqualTo(expectedValue);
        }

        public SpyWebSocketClient SpyWS { get; set; }
        public TestScheduler VirtualScheduler { get; set; }

        [SetUp]
        public void Init()
        {
            VirtualScheduler = new TestScheduler();
            SpyWS = new SpyWebSocketClient();
        }
    }
}