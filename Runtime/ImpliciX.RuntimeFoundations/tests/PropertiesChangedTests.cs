using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.Elements;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RuntimeFoundations.Tests
{
  [TestFixture]
  public class PropertiesChangedTests
  {
    [Test]
    [Category("ExcludeFromCI")]
    public void properties_changed_should_contain_unique_properties()
    {
      var fp = new List<DataModelValue<int>>()
      {
        new DataModelValue<int>("urn:a", 2, TimeSpan.Zero),
        new DataModelValue<int>("urn:b", 42, TimeSpan.Zero),
        new DataModelValue<int>("urn:a", 0, TimeSpan.Zero),
        new DataModelValue<int>("urn:b", -42, TimeSpan.Zero),
        new DataModelValue<int>("urn:c", 80, TimeSpan.Zero),
      };
      Check.ThatCode(() => PropertiesChanged.Create(fp, TimeSpan.Zero))
        .Throws<ContractException>()
        .WithMessage("Contract is not satisfied. Concurrent changes detected for urns: urn:a; urn:b");
    }

    [Test]
    public void properties_changed_get_property()
    {
      var sut = PropertiesChanged.Create(lightning.interior.kitchen.settings.mode, ControlMode.Manual, TimeSpan.Zero);
      var resultNone = sut.GetPropertyValue<PowerConsumption>(lightning.interior.kitchen.consumption);
      var resultSome = sut.GetPropertyValue<ControlMode>(lightning.interior.kitchen.settings.mode);
      Check.That(resultNone.IsNone).IsTrue();
      Check.That(resultSome.IsSome).IsTrue();
      Check.That(resultSome.GetValue()).IsEqualTo(ControlMode.Manual);
    }

    [Test]
    public void properties_changed_contains_property()
    {
      var sut = PropertiesChanged.Create(lightning.interior.kitchen.settings.mode, ControlMode.Manual, TimeSpan.Zero);
      Check.That(sut.ContainsProperty(lightning.interior.kitchen.consumption)).IsFalse();
      Check.That(sut.ContainsProperty(lightning.interior.kitchen.settings.mode)).IsTrue();
      Check.That(sut.ContainsProperty(lightning.interior.kitchen.settings.mode, ControlMode.Automatic)).IsFalse();
      Check.That(sut.ContainsProperty(lightning.interior.kitchen.settings.mode, ControlMode.Manual)).IsTrue();
    }

    [Test]
    public void properties_changed_contains_optional_group()
    {
      var lightsData = new List<DataModelValue<LightStatus>>()
      {
        new DataModelValue<LightStatus>(lightning.interior.kitchen.lights._1.status, LightStatus.On, TimeSpan.Zero),
        new DataModelValue<LightStatus>(lightning.interior.kitchen.lights._2.status, LightStatus.Off, TimeSpan.Zero),
      };
      var sut = PropertiesChanged.Create(lightning.interior.kitchen.lights.Urn, lightsData, TimeSpan.Zero);
      Check.That(sut.Group).IsEqualTo(lightning.interior.kitchen.lights.Urn);
      Check.That(sut.ModelValues.Count()).IsEqualTo(2);
    }
  }
}