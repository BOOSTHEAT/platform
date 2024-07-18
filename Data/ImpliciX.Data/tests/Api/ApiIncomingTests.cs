using System;
using System.Linq;
using ImpliciX.Data.Api;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.Api;

[TestFixture]
public class ApiIncomingTests
{
  [Test]
  public void decode_properties_message()
  {
    var json = @"{
        ""$type"": ""properties"",
        ""Kind"":""properties"",
        ""Properties"": [
          { ""Urn"":""service:dhw:top_temperature:measure"", ""Value"":""1200"", ""At"":"""" },
          { ""Urn"":""service:dhw:top_temperature:status"", ""Value"":""Success"", ""At"":"""" }
        ]
      }";
    var called = false;
    WebsocketApiV2
      .FromJson(json)
      .OnPrelude(_ => throw new Exception("This code shall not be called on a properties message"))
      .OnProperties(properties =>
      {
        var props = properties as Property[] ?? properties.ToArray();
        Check.That(props).HasSize(2);
        Check.That(props.First().Urn).IsEqualTo("service:dhw:top_temperature:measure");
        Check.That(props.First().Value).IsEqualTo("1200");
        Check.That(props.Last().Urn).IsEqualTo("service:dhw:top_temperature:status");
        Check.That(props.Last().Value).IsEqualTo("Success");
        called = true;
      });
    Check.That(called).IsTrue();
  }

  [Test]
  public void decode_prelude_message()
  {
    var json =@"{
      ""$type"": ""prelude"",
      ""Name"": ""yolo"",
      ""Version"": ""6.4.2.888"",
      ""Setup"": ""foo"",
      ""Setups"": [ ""foo"", ""bar"" ],
      ""Kind"": ""prelude""
    }";
    var called = false;
    WebsocketApiV2
      .FromJson(json)
      .OnProperties(_ => throw new Exception("This code shall not be called on a prelude message"))
      .OnPrelude(prelude =>
      {
        Check.That(prelude.Name).IsEqualTo("yolo");
        Check.That(prelude.Version).IsEqualTo("6.4.2.888");
        Check.That(prelude.Setup).IsEqualTo("foo");
        Check.That(prelude.Setups).IsEqualTo(new [] {"foo", "bar"});
        called = true;
      });
    Check.That(called).IsTrue();
  }
}