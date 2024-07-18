using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.SharedKernel.Scheduling;
using ImpliciX.SharedKernel.Tools;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.Driver.Dumb
{
  public delegate TimeSpan Clock();

  public class DumbDriverModule : ImpliciXModule
  {
    public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef) =>
      new DumbDriverModule(moduleName, rtDef.ModelDefinition,
        rtDef.Module<DriverSimulationModuleDefinition>().Properties);

    public DumbDriverModule(string id, Assembly modelDefinition,
      Func<IPropertySimulation, Func<TimeSpan, IEnumerable<IDataModelValue>>[]> simulatedProperties) : base(id)
    {
      DefineModule
      (
        initDependencies: cfg => cfg.AddSettings<DumbDriverSettings>("Modules", id),
        initResources: provider =>
        {
          return new object[]
          {
            provider.GetSettings<DumbDriverSettings>(Id),
            provider.GetService<IClock>()
          };
        },
        createFeature: assets =>
        {
          var settings = assets.Get<DumbDriverSettings>();
          var modelFactory = new ModelFactory(modelDefinition);
          var clock = assets.Get<IClock>();
          var simulation = simulatedProperties(new PropertiesSimulation());
          var rootUrn = simulation
            .SelectMany(x => x(TimeSpan.Zero))
            .Select(m => m.Urn)
            .FindRoot();
          return DefineFeature()
            .Handles<SystemTicked>(@event => HandleSystemTicked(@event, settings, clock))
            .Handles<QueryStateRequested>(ReadState(clock.Now, rootUrn, simulation))
            .Handles<Idle>(Wait(clock.Now, (int)settings.ReadPaceInSystemTicks * 1000))
            .Handles<CommandRequested>(DumbActuators.ExecuteCommand(clock.Now, modelFactory), _ => true)
            .Create();
        });

      DefineSchedulingUnit(
        assets => schedulingUnit => { },
        _ => __ => { });
    }

    private static DomainEventHandler<QueryStateRequested> ReadState(
      Clock clock,
      Urn rootUrn,
      Func<TimeSpan, IEnumerable<IDataModelValue>>[] simulatedProperties
      ) =>
      (_) =>
      {
        var time = clock();
        var measurements = simulatedProperties.SelectMany(x => x(time));
        return new DomainEvent[]
        {
          PropertiesChanged.Create(rootUrn, measurements, time),
        };
      };
    
    private DomainEvent[] HandleSystemTicked(SystemTicked trigger, DumbDriverSettings settings, IClock clock)
    {
      if (trigger.TickCount % settings.ReadPaceInSystemTicks == 0)
        return new DomainEvent[]
        {
          new QueryStateRequested(clock.Now())
        };
      return Array.Empty<DomainEvent>();
    }

    private static DomainEventHandler<Idle> Wait(Func<TimeSpan> now, int readPeriodMilliseconds) =>
      idle =>
      {
        var driversSleepingTime = readPeriodMilliseconds - idle.MailboxDrainingDuration;

        if (driversSleepingTime > 0)
        {
          Thread.Sleep(driversSleepingTime);
        }
        else
        {
          Log.Warning(
            $"DumbDriver not sleeping. Mailbox drained in {idle.MailboxDrainingDuration} ms. Idle cycle duration is {idle.IdleCycleDuration} ms");
        }

        return new DomainEvent[]
        {
          new QueryStateRequested(now())
        };
      };
  }
}