using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;
using Moq;
using ImpliciX.Language;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Api.TcpModbus.Tests;

[TestFixture]
public class ModbusPublisherTests
{
  [Test]
  public void AllReceivedValuesArePublished()
  {
    RaiseSlaveEvent(1, 59392, 17945, 59292, 17946);
    RaiseSlaveEvent(822, 59392, 17945, 59292, 17946);

    var result = GetDomainEventsFromPublisher();
    Assert.That(result.Length, Is.EqualTo(1));
    var values = ((PropertiesChanged)result[0]).ModelValues.ToArray();
    Assert.That(values.Length, Is.EqualTo(4));

    Assert.AreEqual(values[0].Urn, test_model.temp1.measure);
    Assert.AreEqual(values[0].ModelValue(), Temperature.Create(9850));
    Assert.AreEqual(values[1].Urn, test_model.temp2.measure);
    Assert.AreEqual(values[1].ModelValue(), Temperature.Create(9913.90234f));
    Assert.AreEqual(values[2].Urn,
      MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString()));
    Assert.AreEqual(values[2].ModelValue(), 
      new MetricValue(9850, TimeSpan.Zero, TimeSpan.Zero));
    Assert.AreEqual(values[3].Urn,
      MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString()));
    Assert.AreEqual(values[3].ModelValue(), 
      new MetricValue(9913.90234f, TimeSpan.Zero, TimeSpan.Zero));
  }
  
  [Test]
  public void SamePropertyCanOnlyBeChangedOncePerCycle()
  {
    RaiseSlaveEvent(1, 0, 0, 0, 0);
    RaiseSlaveEvent(1, 59392, 17945, 59292, 17946);

    var result = GetDomainEventsFromPublisher();
    Assert.That(result.Length, Is.EqualTo(1));
    var values = ((PropertiesChanged)result[0]).ModelValues.ToArray();
    Assert.That(values.Length, Is.EqualTo(2));

    Assert.AreEqual(values[0].Urn, test_model.temp1.measure);
    Assert.AreEqual(values[0].ModelValue(), Temperature.Create(9850));
    Assert.AreEqual(values[1].Urn, test_model.temp2.measure);
    Assert.AreEqual(values[1].ModelValue(), Temperature.Create(9913.90234f));
  }

  [Test]
  public void NoEvent()
  {
    var result = GetDomainEventsFromPublisher();

    Assert.AreEqual(result.Length, 0);
  }

  [Test]
  public void UnexpectedValues()
  {
    RaiseSlaveEvent(1, 59392);

    var result = GetDomainEventsFromPublisher();

    Assert.AreEqual(result.Length, 0);
  }

  [Test]
  public void MixOfExpectedAndUnexpectedValues()
  {
    RaiseSlaveEvent(1, 59392, 17945, 59292);
    RaiseSlaveEvent(822, 59392, 17945, 59292, 17946);

    var result = GetDomainEventsFromPublisher();

    Assert.That(result.Length, Is.EqualTo(1));
    var values = ((PropertiesChanged)result[0]).ModelValues.ToArray();
    Assert.That(values.Length, Is.EqualTo(3));
    
    Assert.That(values[0].Urn, Is.EqualTo(test_model.temp1.measure));
    Assert.That(values[0].ModelValue(), Is.EqualTo(Temperature.Create(9850)));
    Assert.That(values[1].Urn, Is.EqualTo(MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString())));
    Assert.That(values[1].ModelValue(), Is.EqualTo(new MetricValue(9850, TimeSpan.Zero, TimeSpan.Zero)));
    Assert.That(values[2].Urn, Is.EqualTo(MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString())));
    Assert.That(values[2].ModelValue(), Is.EqualTo(new MetricValue(9913.902f, TimeSpan.Zero, TimeSpan.Zero)));
  }

  [Test]
  public void PersistentChangeRequest()
  {
    RaiseSlaveEvent(150, 48759, 15903);

    var result = GetDomainEventsFromPublisher();

    Assert.That(result.Length, Is.EqualTo(1));
    var settings = ((PersistentChangeRequest)result[0]).ModelValues.ToArray();
    Assert.That(settings.Length, Is.EqualTo(1));
    Assert.That(settings[0].Urn, Is.EqualTo(test_model.threshold));
    Assert.That(settings[0].ModelValue(), Is.EqualTo(Percentage.FromFloat(0.156f).Value));
  }

  [Test]
  public void MixOfPropertiesChangedAndPersistentChangeRequest()
  {
    RaiseSlaveEvent(1, 59392, 17945);
    RaiseSlaveEvent(150, 48759, 15903);

    var result = GetDomainEventsFromPublisher();

    Assert.That(result.Length, Is.EqualTo(2));
    
    var properties = ((PropertiesChanged)result[0]).ModelValues.ToArray();
    Assert.That(properties.Length, Is.EqualTo(1));
    Assert.That(properties[0].Urn, Is.EqualTo(test_model.temp1.measure));
    Assert.That(properties[0].ModelValue(), Is.EqualTo(Temperature.Create(9850)));
    
    var settings = ((PersistentChangeRequest)result[1]).ModelValues.ToArray();
    Assert.That(settings.Length, Is.EqualTo(1));
    Assert.That(settings[0].Urn, Is.EqualTo(test_model.threshold));
    Assert.That(settings[0].ModelValue(), Is.EqualTo(Percentage.FromFloat(0.156f).Value));
  }

  [Test]
  public void SingleCommandRequested()
  {
    RaiseSlaveEvent(200, 48759, 15903);

    var result = GetDomainEventsFromPublisher();

    Assert.That(result.Length, Is.EqualTo(1));
    var command = (CommandRequested)result[0];
    Assert.That(command.Urn, Is.EqualTo(test_model.change));
    Assert.That(command.Arg, Is.EqualTo(Percentage.FromFloat(0.156f).Value));
  }

  [Test]
  public void MultipleCommandRequested()
  {
    RaiseSlaveEvent(200, 48759, 15903);
    RaiseSlaveEvent(200, 18350, 16193);

    var result = GetDomainEventsFromPublisher();

    Assert.That(result.Length, Is.EqualTo(2));

    var command0 = (CommandRequested)result[0];
    Assert.That(command0.Urn, Is.EqualTo(test_model.change));
    Assert.That(command0.Arg, Is.EqualTo(Percentage.FromFloat(0.156f).Value));

    var command1 = (CommandRequested)result[1];
    Assert.That(command1.Urn, Is.EqualTo(test_model.change));
    Assert.That(command1.Arg, Is.EqualTo(Percentage.FromFloat(0.755f).Value));
  }

  private Mock<IModbusTcpSlaveAdapter> _slaveMock;
  private ModbusPublisher _sut;

  [SetUp]
  public void Initialize()
  {
    _slaveMock = new Mock<IModbusTcpSlaveAdapter>();
    _sut = new ModbusPublisher(_slaveMock.Object, () => TimeSpan.Zero);
  }

  private void RaiseSlaveEvent(ushort startAddress, params ushort[] data)
  {
    _slaveMock.Raise(
      m => m.OnHoldingRegisterUpdate += null,
      _slaveMock.Object,
      (startAddress, data)
    );
  }

  private DomainEvent[] GetDomainEventsFromPublisher() => _sut.PullDomainEvents(CreateModbusMapping());

  private static ModbusMapping CreateModbusMapping()
  {
    var registerToUrn = new Dictionary<Urn, ushort>
    {
      { test_model.temp1.measure, 1 },
      { test_model.temp2.measure, 3 },
      { test_model.temp3.measure, 4 },
      { MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString()), 822 },
      { MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString()), 824 },
      { test_model.threshold, 150 },
      { test_model.change, 200 },
    };
    var modbusMapDefinition = new TcpModbusApiModuleDefinition();
    modbusMapDefinition.MeasuresMap = registerToUrn;
    modbusMapDefinition.AlarmsMap = new Dictionary<Urn, ushort>();
    var modbusMapping = new ModbusMapping(modbusMapDefinition);
    return modbusMapping;
  }
}