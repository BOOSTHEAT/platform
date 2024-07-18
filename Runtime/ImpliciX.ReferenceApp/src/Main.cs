using System;
using System.Collections.Generic;
using System.Reflection;
using ImpliciX.Language;
using ImpliciX.Language.Control;
using ImpliciX.Language.Driver;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;
using ImpliciX.Language.Records;
using ImpliciX.Language.StdLib;
using ImpliciX.Language.Store;
using ImpliciX.ReferenceApp.App;
using ImpliciX.ReferenceApp.Model;
using ImpliciX.ReferenceApp.Model.Enums;

namespace ImpliciX.ReferenceApp;

public class Main : ApplicationDefinition
{
  public Main()
  {
    AppName = "ReferenceApp";
    AppSettingsFile = "appsettings.json";

    DataModelDefinition = ModuleDefinition.DataModel(device._);

    ModuleDefinitions = new object[]
    {
      new ControlModuleDefinition { Assembly = Assembly.GetExecutingAssembly() },

      new PersistentStoreModuleDefinition
      {
        DefaultUserSettings = DefaultValues.UserSettings,
        DefaultVersionSettings = DefaultValues.VersionSettings,
        CleanVersionSettings = device._._clean_version_settings
      },

      new DriverSimulationModuleDefinition
      {
        Properties = sim => new[]
        {
          sim.Stepper(monitoring.pulse_counter.gas_index.measure, 100, TimeSpan.FromSeconds(1), 0.01),
          sim.Discrete(monitoring.pulse_counter.gas_index.status, MeasureStatus.Success),
          sim.Stepper(monitoring.pulse_counter.electrical_index.measure, 0, TimeSpan.FromSeconds(1), 400),
          sim.Discrete(monitoring.pulse_counter.electrical_index.status, MeasureStatus.Success),
          sim.Sinusoid(monitoring.dhw.power, 0, 100, 1.0),
          sim.Sinusoid(monitoring.heating.power, 0, 100, 1.0),
          sim.Sinusoid(monitoring.dhw.energy, 0, 100, 1.0),
          sim.Sinusoid(monitoring.heating.energy, 0, 100, 1.0),
          sim.Discrete(monitoring.product.heating_service_state.measure, States.Running,
            (States.Running, 0.4),
            (States.Disabled, 0.3),
            (States.Failure, 0.3)),
          sim.Sinusoid(monitoring.product.compressor_running_time, 0, 100, 1.0)
        }
      },
      
      new DriverModbusModuleDefinition
      {
        ModbusSlavesManagement = new ModbusSlaveModel
          { Commit = device._.software._commit_update, Rollback = device._.software._rollback_update },
        Slaves = new []
        {
          SomeSlave.Definition,
        }
      },

      ModuleDefinition.SystemSoftware(device._, s => s == AppName),
      ModuleDefinition.MmiHost(device._),

      new MetricsModuleDefinition
      {
        Metrics = AllMetrics.Declarations,
        SnapshotInterval = AllMetrics.SnapshotInterval
      },

      new TimeMathModuleDefinition
      {
        Metrics = AllTimeMath.Declarations
      },

      new TimeCapsuleDefinition
      {
        Metrics = AllMetrics.Declarations,
        UserInterface = () => new GUI(),
      },
      
      new FrozenTimeSeriesDefinition
      {
        TimeSeries = new []
        {
          SomeSlave.Definition.TimeSeries(),
          new MinimalistTimeSeries(monitoring.Self.Urn),
        },
        Metrics = AllMetrics.Declarations
      },
      
      new HttpTimeSeriesDefinition
      {
        TimeSeries = new []
        {
          SomeSlave.Definition.TimeSeries().Over.ThePast(5).Days
        },
        Metrics = AllMetrics.Declarations
      },

      new RecordsModuleDefinition
      {
        Records = AllRecords.Records
      }
    };
  }
}



