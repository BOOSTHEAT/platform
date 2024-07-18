using System;
using System.Collections.Generic;
using ImpliciX.Alarms;
using ImpliciX.Api.TcpModbus;
using ImpliciX.Api.WebSocket;
using ImpliciX.Chronos;
using ImpliciX.CommunicationMetrics;
using ImpliciX.Control;
using ImpliciX.Driver.Dumb;
using ImpliciX.FmuDriver;
using ImpliciX.FrozenTimeSeries;
using ImpliciX.Harmony;
using ImpliciX.HttpTimeSeries;
using ImpliciX.Metrics;
using ImpliciX.MmiHost;
using ImpliciX.Motors.Controllers;
using ImpliciX.PersistentStore;
using ImpliciX.Records;
using ImpliciX.RTUModbus.Controllers;
using ImpliciX.RuntimeFoundations;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SystemSoftware;
using ImpliciX.ThingsBoard;
using ImpliciX.TimeCapsule;
using ImpliciX.TimeMath;
using ImpliciX.TimeSeries;
using ImpliciX.Watchdog;

namespace ImpliciX.Runtime;

internal static class ModuleFactory
{
  private static readonly Dictionary<string, Func<string, ApplicationRuntimeDefinition, IImpliciXModule>>
    ModuleFactories =
      new()
      {
        { "Api", ApiModule.Create },
        { "Chronos", ChronosModule.Create },
        { "PersistentStore", PersistentStoreModule.Create },
        { "Control", ControlModule.Create },
        { "TimeSeries", TimeSeriesModule.Create },
        { "FrozenTimeSeries", FrozenTimeSeriesModule.Create },
        { "RTUModbus", ModbusMasterModule.Create },
        { "MotorsDriver", MotorsDriverModule.Create },
        { "DumbDriver", DumbDriverModule.Create },
        { "FmuDriver", DriverFmuModule.Create },
        { "MmiHost", MmiHostModule.Create },
        { "SystemSoftware", SystemSoftwareModule.Create },
        { "Alarms", AlarmsModule.Create },
        { "Metrics", MetricsModule.Create },
        { "TimeMath", TimeMathModule.Create },
        { "CommunicationMetrics", CommunicationMetricsModule.Create },
        { "ApiModbus", ApiTcpModbusModule.Create },
        { "Watchdog", WatchdogModule.Create },
        { "Harmony", HarmonyModule.Create },
        { "ThingsBoard", ThingsBoardModule.Create },
        { "TimeCapsule", TimeCapsuleModule.Create },
        { "Records", RecordsModule.Create },
        { "Grafana", HttpTimeSeriesModule.Create },
        { "HttpTimeSeries", HttpTimeSeriesModule.Create },
      };

  public static IImpliciXModule Create(string factoryName, string moduleName, ApplicationRuntimeDefinition rtDef)
  {
    return ModuleFactories.TryGetValue(factoryName,
      out var factory)
      ? factory(moduleName, rtDef)
      : throw new Exception($"Cannot find factory named {factoryName} for module {moduleName}");
  }
}