using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RuntimeFoundations.Tests
{
  [TestFixture]
  public class EqualityTest
  {
    [TestCase(new[] { "c0", "c1", "c2" }, new[] { 45f, -2f, 5.3f })]
    [TestCase(new[] { "c1", "c0", "c2" }, new[] { -2f, 45f, 5.3f })]
    [TestCase(new[] { "c1", "c2", "c0" }, new[] { -2f, 5.3f, 45f })]
    public void function_definition_should_be_equals(string[] paramsName, float[] paramsValue)
    {
      var functionDefinition = GetFunctionDefinitionFromParams(paramsName, paramsValue);
      var functionDefinition2 = new FunctionDefinition(new[] { ("c0", 45f), ("c1", -2f), ("c2", 5.3f) });

      Check.That(functionDefinition.GetHashCode()).IsEqualTo(functionDefinition2.GetHashCode());
      Check.That(functionDefinition).IsEqualTo(functionDefinition2);
    }

    [TestCase(new[] { "c0", "c1" }, new[] { 44f, -222f })]
    [TestCase(new[] { "c1", "c1" }, new[] { 44f, -2f })]
    [TestCase(new[] { "c2", "c1" }, new[] { 44f, -2f })]
    [TestCase(new[] { "c1", "c0" }, new[] { 44f, -2f })]
    [TestCase(new[] { "c0", "c3" }, new[] { 44f, -222f })]
    [TestCase(new[] { "c0", "c3" }, new[] { 54f, -222f })]
    public void function_definition_should_not_be_equals_when_hashcodes_differ(string[] paramsName, float[] paramsValue)
    {
      var functionDefinition = GetFunctionDefinitionFromParams(paramsName, paramsValue);
      var functionDefinition2 = new FunctionDefinition(new[] { ("c0", 44f), ("c1", -2f) });

      Check.That(functionDefinition.GetHashCode()).IsNotEqualTo(functionDefinition2.GetHashCode());
      Check.That(functionDefinition).IsNotEqualTo(functionDefinition2);
    }

    private FunctionDefinition GetFunctionDefinitionFromParams(string[] paramsName, float[] paramsValue)
    {
      return new FunctionDefinition(paramsName.Zip(paramsValue, (First, Second) => (First, Second)).ToArray());
    }

    [Test]
    public void function_definition_should_not_be_equals_even_with_same_hashcode()
    {
      var pairs = GetStringsWithSameHashcode(3);
      var functionDefinition = new FunctionDefinition(new[] { (pairs[0].s1, 45f), (pairs[1].s1, -2f), (pairs[2].s1, 5.3f) });
      var functionDefinition2 = new FunctionDefinition(new[] { (pairs[0].s2, 45f), (pairs[1].s2, -2f), (pairs[2].s2, 5.3f) });

      Check.That(functionDefinition.GetHashCode()).IsEqualTo(functionDefinition2.GetHashCode());
      Check.That(functionDefinition).IsNotEqualTo(functionDefinition2);
    }
    
    private List<(string s1,string s2, int h)> GetStringsWithSameHashcode(int count)
    {
      var r = new Random();
      char rc() => (char)r.Next('a', 'z' + 1);
      var lookup = new Dictionary<int, string>();
      var pairs = new List<(string, string, int)>();

      while (true)
      {
        var s1 = rc() + rc().ToString() + rc() + rc() + rc() + rc();
        var h = s1.GetHashCode();
        if (lookup.TryGetValue(h, out string s2) && s2 != s1)
        {
          pairs.Add((s1,s2,h));
          if (pairs.Count >= count)
            return pairs;
          continue;
        }
        lookup[h] = s1;
      }
    }

    [Test]
    public void boilerStateChanged_should_be_equals()
    {
      var modelsValues = new List<IDataModelValue>
      {
        Property<Temperature>.Create(PropertyUrn<Temperature>.Build("UrnTemperature"), Temperature.Create(45f),
          TimeSpan.Zero)
      };

      var modelsValues2 = new List<IDataModelValue>
      {
        Property<Temperature>.Create(PropertyUrn<Temperature>.Build("UrnTemperature"), Temperature.Create(45f),
          TimeSpan.Zero)
      };

      var event1 = PropertiesChanged.Create(modelsValues, TimeSpan.FromDays(45));
      var event2 = PropertiesChanged.Create(modelsValues2, TimeSpan.FromDays(45));

      Check.That(event1).IsEqualTo(event2);
    }

    [Test]
    public void dataModelValue_should_be_equals()
    {
      var fake = new DataModelValue<object>("test", 45, TimeSpan.Zero);
      var fake2 = new DataModelValue<object>("test", 45, TimeSpan.Zero);
      Check.That(fake).IsEqualTo(fake2);
    }

    [Test]
    public void boilerStateChanged_with_different_source_event_should_be_equals()
    {
      var modelsValues = new List<IDataModelValue>
      {
        Property<Temperature>.Create(PropertyUrn<Temperature>.Build("UrnTemperature"), Temperature.Create(45f),
          TimeSpan.Zero)
      };

      var event1 = PropertiesChanged.Create(modelsValues, TimeSpan.FromDays(45));
      var event2 = PropertiesChanged.Create(modelsValues, TimeSpan.FromDays(45));

      Check.That(event1).IsEqualTo(event2);
    }

    [TestCase("UrnTemperature2", 45f, 45, 45)]
    [TestCase("UrnTemperature", 46f, 45, 45)]
    public void boilerStateChanged_should_not_be_equals(string urn, float temperature, int timespan, int timespan2)
    {
      var modelsValues = new List<IDataModelValue>
      {
        Property<Temperature>.Create(PropertyUrn<Temperature>.Build("UrnTemperature"), Temperature.Create(45f),
          TimeSpan.FromSeconds(45))
      };

      var modelsValues2 = new List<IDataModelValue>
      {
        Property<Temperature>.Create(PropertyUrn<Temperature>.Build(urn), Temperature.Create(temperature),
          TimeSpan.FromSeconds(timespan))
      };

      var event1 = PropertiesChanged.Create(modelsValues, TimeSpan.FromDays(45));
      var event2 = PropertiesChanged.Create(modelsValues2, TimeSpan.FromDays(timespan2));

      Check.That(event1).IsNotEqualTo(event2);
    }


    [TestCase("UrnTest2", "valueTest", false)]
    [TestCase("UrnTest", "valueTest2", false)]
    [TestCase("UrnTest", "valueTest", true)]
    public void CommandRequested_should_be_equals(string otherUrn, string otherValue, bool shouldBeEqual)
    {
      var @this = CommandRequested.Create(CommandUrn<string>.Build("UrnTest"), "valueTest", TimeSpan.FromDays(45));
      var other = CommandRequested.Create(CommandUrn<string>.Build(otherUrn), otherValue, TimeSpan.FromDays(45));
      Check.That(@this.Equals(other)).IsEqualTo(shouldBeEqual);
    }


    [Test]
    public void timeoutOccured_should_be_equals()
    {
      var requestId = Guid.NewGuid();
      var event1 = TimeoutOccured.Create("UrnTest", TimeSpan.FromDays(45), requestId);
      var event2 = TimeoutOccured.Create("UrnTest", TimeSpan.FromDays(45), requestId);

      Check.That(event1).IsEqualTo(event2);
    }

    [Test]
    public void timeoutOccured_and_command_request_should_not_be_equal()
    {
      var event1 = TimeoutOccured.Create("UrnTest", TimeSpan.FromDays(45), Guid.Empty);
      var event2 = CommandRequested.Create(CommandUrn<string>.Build("UrnTest"), "a", TimeSpan.FromDays(45));
      Check.That(event1).IsNotEqualTo(event2);
    }

    [TestCase("UrnTest2", 45)]
    public void timeoutOccured_should_not_be_equals(string urn1, int at)
    {
      var event1 = TimeoutOccured.Create("UrnTest", TimeSpan.FromDays(45), Guid.NewGuid());
      var event2 = TimeoutOccured.Create(urn1, TimeSpan.FromDays(at), Guid.NewGuid());

      Check.That(event1).IsNotEqualTo(event2);
    }


    [TestCase(new[] { "x", "y" }, true)]
    [TestCase(new[] { "x", "Y" }, true)]
    [TestCase(new[] { "x", "y1" }, false)]
    public void urn_equality_should_be_case_case_insesitive(string[] parts, bool expected)
    {
      Urn u1 = Urn.BuildUrn(parts);
      Urn u2 = Urn.BuildUrn("x:y");
      Check.That(u1 == u2).IsEqualTo(expected);
    }


    [TestCase(new[] { "x", "y" }, true)]
    [TestCase(new[] { "x", "Y" }, true)]
    [TestCase(new[] { "x", "y1" }, false)]
    public void urn_value_equality_should_be_case_case_insesitive(string[] parts, bool expected)
    {
      Urn u1 = Urn.BuildUrn(parts);
      Urn u2 = Urn.BuildUrn("x:y");
      Check.That(u1.ValueEquals(u2)).IsEqualTo(expected);
    }

    [Test]
    [Ignore("bug-wip")]
    public void urn_equality_is_checking_urn_type()
    {
      var u1 = PropertyUrn<string>.Build("x", "y");
      var u2 = PropertyUrn<int>.Build("x", "y");
      Check.That(u1).IsNotEqualTo(u2);

      var u3 = PropertyUrn<string>.Build("x", "y");
      Check.That(u1).IsEqualTo(u3);
    }
  }
}