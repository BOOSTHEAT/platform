using ImpliciX.Api.TcpModbus;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NModbus;
using NUnit.Framework;
using Moq;
using System.Linq;
using ImpliciX.Api.TcpModbus.Infrastructure;

namespace ImpliciX.Api.TcpModbus.Tests;

[TestFixture]
public class ApiTcpModbusServiceTests
{
  [Test]
  public void should_save_measures_to_holding_registers()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);

    apiTcpModbusService.HandlePropertiesChanged(MappedMeasuresChanged);

    Check.That(slaveAdapter.GetHoldingRegisterValue(1)).IsEqualTo(new ushort[] { 0, 16880 });
    Check.That(slaveAdapter.GetHoldingRegisterValue(2)).IsEqualTo(new ushort[] { 0, 16800 });
  }

  [Test]
  public void should_save_enum_counters_to_holding_registers()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    apiTcpModbusService.HandlePropertiesChanged(MappedCountersChanged);
    Check.That(slaveAdapter.GetHoldingRegisterValue(20)).IsEqualTo(new ushort[] { 0, 16936 });
    Check.That(slaveAdapter.GetHoldingRegisterValue(22)).IsEqualTo(new ushort[] { 0, 16672 });
  }

  [Test]
  public void should_save_alarms_states_to_discrete_inputs()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);

    apiTcpModbusService.HandlePropertiesChanged(MappedAlarmsChanged);

    Check.That(slaveAdapter.GetDiscreteInputValue(11)).IsEqualTo(new[] { true });
    Check.That(slaveAdapter.GetDiscreteInputValue(12)).IsEqualTo(new[] { false });
  }

  [Test]
  public void should_start_handling_modbus_request_when_presence_enabled()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    apiTcpModbusService.HandlePropertiesChanged(MappedMeasuresChanged);
    apiTcpModbusService.HandlePropertiesChanged(EnableTrigger);
    Check.That(slaveAdapter.HasBeenStarted).IsTrue();
  }

  [Test]
  public void should_stop_handling_modbus_request_when_presence_disabled()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    apiTcpModbusService.HandlePropertiesChanged(DisableTrigger);
    Check.That(slaveAdapter.HasBeenStopped).IsTrue();
  }

  [Test]
  public void can_handle_properties_changed_containing_settings()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    Check.That(apiTcpModbusService.CanHandle(EnableTrigger)).IsTrue();
  }

  [Test]
  public void can_handle_properties_changed_containing_mapped_measures()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    Check.That(apiTcpModbusService.CanHandle(MappedMeasuresChanged)).IsTrue();
  }

  [Test]
  public void cannot_handle_properties_changed_containing_only_not_mapped_properties()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    Check.That(apiTcpModbusService.CanHandle(OnlyNotMappedPropertiesChanged)).IsFalse();
  }

  [Test]
  public void can_handle_properties_changed_containing_mapped_and_not_mapped_properties()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    Check.That(apiTcpModbusService.CanHandle(MappedAndNotMappedPropertiesChanged)).IsTrue();
  }

  [Test]
  [Ignore("Uses real tcp modbus infrastructure")]
  public void examples()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new ModbusTcpSlaveAdapter(Settings);
    var apiTcpModbusService = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    apiTcpModbusService.HandlePropertiesChanged(
      PropertiesChanged.Create(test_model.temp3.measure, Temperature.Create(2389.5f), TimeSpan.Zero));
    apiTcpModbusService.HandlePropertiesChanged(EnableTrigger);
    apiTcpModbusService.HandlePropertiesChanged(MappedAlarmsChanged);

    ushort[] registers = null;
    bool[] discreteInputs = null;
    Check.ThatCode(() =>
    {
      Thread.Sleep(500);
      using var master = CreateModbusMaster();
      master.Transport.ReadTimeout = 100;
      registers = master.ReadHoldingRegisters(Settings.SlaveId, 4, 2);
      discreteInputs = master.ReadCoils(Settings.SlaveId, 11, 2);
    }).Not.ThrowsAny();

    var highRegister = BitConverter.GetBytes(registers[0]);
    var lowRegister = BitConverter.GetBytes(registers[1]);

    var decodeFloatValue = BitConverter.ToSingle(new[]
      { highRegister[0], highRegister[1], lowRegister[0], lowRegister[1] });
    Check.That(decodeFloatValue).IsEqualTo(2389.5f);
    Check.That(discreteInputs).ContainsExactly(true, false);
  }


  private static ModbusTcpSettings Settings => new ModbusTcpSettings { SlaveId = 1, TCPPort = 5002 };

  private static ModbusMapping ModbusMapping => new ModbusMapping(new TcpModbusApiModuleDefinition()
  {
    Presence = test_model.presence,
    MeasuresMap = new Dictionary<Urn, ushort>
    {
      { test_model.temp1.measure, 1 },
      { test_model.temp2.measure, 2 },
      { test_model.temp3.measure, 4 },
      { MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString()), 20 },
      { MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString()), 22 }
    },
    AlarmsMap = new Dictionary<Urn, ushort>
    {
      { test_model.c001, 11 },
      { test_model.c002, 12 }
    },
  });


  private static PropertiesChanged MappedMeasuresChanged
  {
    get
    {
      return PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temp1.measure, Temperature.Create(30f), TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temp2.measure, Temperature.Create(20f), TimeSpan.Zero),
        Property<Temperature>.Create(test_model.temp3.measure, Temperature.Create(20f), TimeSpan.Zero),
      }, TimeSpan.Zero);
    }
  }

  private static PropertiesChanged MappedCountersChanged
  {
    get
    {
      return PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<MetricValue>.Create(
          MetricUrn.Build(test_model.counter1, DummyState.A.ToString(), "occurrence"),
          new MetricValue(2, TimeSpan.Zero, TimeSpan.Zero), TimeSpan.Zero),
        Property<MetricValue>.Create(
          MetricUrn.Build(test_model.counter1, DummyState.A.ToString(), "duration"),
          new MetricValue((int)TimeSpan.FromSeconds(10).TotalSeconds, TimeSpan.Zero, TimeSpan.Zero),
          TimeSpan.Zero),
        Property<MetricValue>.Create(
          MetricUrn.Build(test_model.counter1, DummyState.B.ToString(), "occurrence"),
          new MetricValue(1, TimeSpan.Zero, TimeSpan.Zero), TimeSpan.Zero),
        Property<MetricValue>.Create(
          MetricUrn.Build(test_model.counter1, DummyState.B.ToString(), "duration"),
          new MetricValue((int)TimeSpan.FromSeconds(42).TotalSeconds, TimeSpan.Zero, TimeSpan.Zero),
          TimeSpan.Zero),
      }, TimeSpan.Zero);
    }
  }

  private static PropertiesChanged MappedAlarmsChanged
  {
    get
    {
      return PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<AlarmState>.Create(test_model.c001, AlarmState.Active, TimeSpan.Zero),
        Property<AlarmState>.Create(test_model.c002, AlarmState.Inactive, TimeSpan.Zero)
      }, TimeSpan.Zero);
    }
  }

  private static PropertiesChanged OnlyNotMappedPropertiesChanged
  {
    get
    {
      return PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.not_mapped.measure, Temperature.Create(30f), TimeSpan.Zero),
      }, TimeSpan.Zero);
    }
  }

  private static PropertiesChanged MappedAndNotMappedPropertiesChanged
  {
    get
    {
      return PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temp1.measure, Temperature.Create(30f), TimeSpan.Zero),
        Property<Temperature>.Create(test_model.not_mapped.measure, Temperature.Create(30f), TimeSpan.Zero),
      }, TimeSpan.Zero);
    }
  }

  private static PropertiesChanged EnableTrigger
  {
    get
    {
      return PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temp1.measure, Temperature.Create(30f), TimeSpan.Zero),
        Property<Presence>.Create(test_model.presence, Presence.Enabled, TimeSpan.Zero),
      }, TimeSpan.Zero);
    }
  }

  private static PropertiesChanged DisableTrigger
  {
    get
    {
      return PropertiesChanged.Create(new IDataModelValue[]
      {
        Property<Temperature>.Create(test_model.temp1.measure, Temperature.Create(30f), TimeSpan.Zero),
        Property<Presence>.Create(test_model.presence, Presence.Disabled, TimeSpan.Zero),
      }, TimeSpan.Zero);
    }
  }

  private IModbusMaster CreateModbusMaster() =>
    new ModbusFactory().CreateMaster(new TcpClient("127.0.0.1", Settings.TCPPort));


  [Test()]
  public void HandleSystemTickedNotNullTest()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new FakeModbusTcpSlaveAdapter();
    var sut = new ApiTcpModbusService(ModbusMapping, slaveAdapter, mockClock.Object);
    SystemTicked tick = SystemTicked.Create(
      TimeSpan.Zero,
      100,
      60 * 10
    );
    var result = sut.HandleSystemTicked(
      tick
    );

    Assert.NotNull(result);
    Assert.AreEqual(result.Length, 0);
  }

  [Test]
  public void HandelSystemTickeTestV2()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new Mock<IModbusTcpSlaveAdapter>();

    var sut = new ApiTcpModbusService(ModbusMapping, slaveAdapter.Object, mockClock.Object);
    slaveAdapter.Raise(m => m.OnHoldingRegisterUpdate += null, slaveAdapter.Object,
      ((ushort)1, new ushort[] { (ushort)59392, (ushort)17945, (ushort)59292, (ushort)17946 }));
    slaveAdapter.Raise(m => m.OnHoldingRegisterUpdate += null, slaveAdapter.Object,
      ((ushort)20, new ushort[] { (ushort)59392, (ushort)17945, (ushort)59292, (ushort)17946 }));

    var result = sut.HandleSystemTicked(
      SystemTicked.Create(
        TimeSpan.Zero,
        100,
        60 * 10
      )
    );

    Assert.NotNull(result);
    Assert.AreEqual(result.Length, 1);

    var result2 = result[0] as PropertiesChanged;

    Assert.AreEqual(result2.ModelValues.Count(), 3);
    Assert.AreEqual(result2.ModelValues.ElementAt(0).Urn, test_model.temp1.measure);
    Assert.AreEqual(result2.ModelValues.ElementAt(0).ModelValue(), Temperature.Create(9850));
    Assert.AreEqual(result2.ModelValues.ElementAt(1).Urn,
      MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString()));
    Assert.AreEqual(result2.ModelValues.ElementAt(1).ModelValue(),
      new MetricValue(9850f, TimeSpan.Zero, TimeSpan.Zero));
    Assert.AreEqual(result2.ModelValues.ElementAt(2).Urn,
      MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString()));
  }

  [Test]
  public void HandelSystemTickeTest2Tick()
  {
    var mockClock = new Mock<Func<TimeSpan>>();
    var slaveAdapter = new Mock<IModbusTcpSlaveAdapter>();

    var sut = new ApiTcpModbusService(ModbusMapping, slaveAdapter.Object, mockClock.Object);
    slaveAdapter.Raise(m => m.OnHoldingRegisterUpdate += null, slaveAdapter.Object,
      ((ushort)1, new ushort[] { (ushort)59392, (ushort)17945, (ushort)59292, (ushort)17946 }));
    slaveAdapter.Raise(m => m.OnHoldingRegisterUpdate += null, slaveAdapter.Object,
      ((ushort)20, new ushort[] { (ushort)59392, (ushort)17945, (ushort)59292, (ushort)17946 }));

    var result = sut.HandleSystemTicked(
      SystemTicked.Create(
        TimeSpan.Zero,
        100,
        60 * 10
      )
    );

    Assert.NotNull(result);
    Assert.AreEqual(result.Length, 1);

    var result2 = result[0] as PropertiesChanged;

    Assert.AreEqual(result2.ModelValues.Count(), 3);
    Assert.AreEqual(result2.ModelValues.ElementAt(0).Urn, test_model.temp1.measure);
    Assert.AreEqual(result2.ModelValues.ElementAt(0).ModelValue(), Temperature.Create(9850));
    Assert.AreEqual(result2.ModelValues.ElementAt(1).Urn,
      MetricUrn.BuildDuration(test_model.counter1, DummyState.B.ToString()));
    Assert.AreEqual(result2.ModelValues.ElementAt(1).ModelValue(),
      new MetricValue(9850f, TimeSpan.Zero, TimeSpan.Zero));
    Assert.AreEqual(result2.ModelValues.ElementAt(2).Urn,
      MetricUrn.BuildDuration(test_model.counter1, DummyState.A.ToString()));

    var resultV2 = sut.HandleSystemTicked(
      SystemTicked.Create(
        TimeSpan.Zero,
        100,
        60 * 10
      )
    );
    Assert.AreEqual(resultV2.Length, 0);
  }
}