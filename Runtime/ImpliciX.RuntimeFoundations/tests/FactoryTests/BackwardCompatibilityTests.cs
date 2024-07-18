using System;
using System.Collections.Generic;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.Elements;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RuntimeFoundations.Tests.FactoryTests
{
  public class BackwardCompatibilityTests
  {
    [Test]
    public void create_properties_with_struct_value_objects()
    {
      var testTime = new TimeSpan(11, 26, 35);
      var backwardCompatibility = new Dictionary<string, Urn>
      {
        {"my_old_app:kitchen:consumption", lightning.interior.kitchen.consumption}
      };
      var sut = new ModelFactory(typeof(BackwardCompatibilityTests).Assembly, backwardCompatibility);

      var result = sut.Create("my_old_app:kitchen:consumption", "156.8", testTime);

      result.CheckIsSuccessAnd((o) =>
      {
        var property = o as Property<PowerConsumption>;
        Check.That(property).IsNotNull();
        Check.That(property?.Urn.Value).IsEqualTo(lightning.interior.kitchen.consumption);
        Check.That(property?.Urn).IsInstanceOf<PropertyUrn<PowerConsumption>>();
        Check.That(property?.Value).IsEqualTo(PowerConsumption.FromFloat(156.8f).Value);
        Check.That(property?.At).IsEqualTo(testTime);

        var nonGenericProp = o as IDataModelValue;
        Check.That(nonGenericProp?.Urn.Value).IsEqualTo(lightning.interior.kitchen.consumption);
        Check.That(nonGenericProp?.ModelValue()).IsEqualTo(PowerConsumption.FromFloat(156.8f).Value);
        Check.That(nonGenericProp?.At).IsEqualTo(testTime);
      });
    }
  }
}