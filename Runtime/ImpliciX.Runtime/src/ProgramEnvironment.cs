using System;
using System.Linq;
using System.Threading;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.RuntimeFoundations;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.IO;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Scheduling;
using Microsoft.Extensions.Configuration;

namespace ImpliciX.Runtime
{
  public class ProgramEnvironment
  {
    public IImpliciXModule[] Modules { get; }
    public IClock Clock { get; }
    public Func<ManualResetEvent, IServiceProvider, ImpliciXScheduler> Scheduler { get; }

    public delegate IImpliciXModule ModuleFactory(string factoryName, string moduleName, ApplicationRuntimeDefinition rtDef);

    public static ProgramEnvironment CreateInstance(IConfiguration configuration, ApplicationDefinition appDef,
      string currentEnvironment, ModuleFactory moduleFactory)
    {
      var setups = configuration.GetSection("Setups");
      var currentSetup = setups.GetSection(currentEnvironment);
      Console.WriteLine($"Boiler App configured with {currentSetup.Key} settings");
      if (!currentSetup.GetChildren().Any())
      {
        Console.WriteLine($"WARNING : The configuration {currentSetup.Key} is not defined in appsettings.json");
      }

      var modulesCatalog = configuration.GetSection("Modules");
      var timeKind = GetTimeKindFromConfig(currentSetup);
      var schedulerKind = GetSchedulerKindFromConfig(currentSetup);
      if (timeKind == TimeKind.Virtual && schedulerKind == SchedulerKind.MultiThreaded)
      {
        throw new Exception($"Incompatible scheduler [{schedulerKind}] and time [{timeKind}]");
      }

      var options = new ApplicationOptions(
        currentSetup.GetSection("Options").GetChildren().ToDictionary(c => c.Key, c => c.Value),
        new EnvironmentService()
      );
      var setupNames = setups.GetChildren().Select(s => s.Key).ToArray();
      var rtDef = new ApplicationRuntimeDefinition(appDef, options, setupNames);
      var modules = GetModules(currentSetup, modulesCatalog, rtDef, moduleFactory);
      var boilerTime = GetTime(timeKind);

      return new ProgramEnvironment(modules, boilerTime,
        GetScheduler(schedulerKind, boilerTime, modules));
    }

    private ProgramEnvironment(IImpliciXModule[] modules, IClock clock,
      Func<ManualResetEvent, IServiceProvider, ImpliciXScheduler> scheduler)
    {
      Modules = modules;
      Clock = clock;
      Scheduler = scheduler;
    }

    private enum TimeKind
    {
      Real,
      Virtual
    }

    private static IClock GetTime(TimeKind timeKind) =>
      timeKind switch
      {
        TimeKind.Real => RealClock.Create(),
        TimeKind.Virtual => VirtualClock.Create(),
        _ => RealClock.Create()
      };

    private static TimeKind GetTimeKindFromConfig(IConfigurationSection setup)
    {
      try
      {
        return Enum.TryParse<TimeKind>(setup["Time"], out var kind)
          ? kind
          : TimeKind.Real;
      }
      catch (Exception e)
      {
        Log.Warning(e.Message);
        return TimeKind.Real;
      }
    }


    private static Func<ManualResetEvent, IServiceProvider, ImpliciXScheduler> GetScheduler(
      SchedulerKind schedulerKind,
      IClock clock, IImpliciXModule[] modules) =>
      (applicationStarted, serviceProvider) => schedulerKind switch
      {
        SchedulerKind.SingleThreaded =>
          new SingleThreadedScheduler(applicationStarted, serviceProvider, modules, clock),
        SchedulerKind.MultiThreaded => new MultiThreadedScheduler(applicationStarted, serviceProvider, modules),
        _ => new MultiThreadedScheduler(applicationStarted, serviceProvider, modules)
      };

    private static SchedulerKind GetSchedulerKindFromConfig(IConfigurationSection setup)
    {
      try
      {
        return Enum.TryParse<SchedulerKind>(setup["Scheduler"], out var kind)
          ? kind
          : SchedulerKind.MultiThreaded;
      }
      catch (Exception e)
      {
        Log.Warning(e.Message);
        return SchedulerKind.MultiThreaded;
      }
    }

    private static IImpliciXModule[] GetModules(IConfigurationSection setup, IConfigurationSection modulesCatalog,
      ApplicationRuntimeDefinition rtdef, ModuleFactory moduleFactory)
    {
      IImpliciXModule GetImpliciXModule(string moduleName)
      {
        var moduleDetails = modulesCatalog.GetSection(moduleName);
        var moduleFactoryName = moduleDetails["Factory"];
        if(moduleFactoryName == null)
          throw new Exception($"No factory defined for module {moduleName}");
        return moduleFactory(moduleFactoryName, moduleName, rtdef);
      }

      var modulesToActivate = setup.GetSection("Modules").GetChildren();
      return modulesToActivate.Select(moduleName => GetImpliciXModule(moduleName.Value)).ToArray();
    }
  }
}