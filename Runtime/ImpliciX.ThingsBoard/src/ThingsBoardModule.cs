using System;
using ImpliciX.Language;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using ImpliciX.ThingsBoard.Infrastructure;
using ImpliciX.ThingsBoard.Messages;
using ImpliciX.ThingsBoard.Publishers;
using ImpliciX.ThingsBoard.States;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;

namespace ImpliciX.ThingsBoard
{
  public class ThingsBoardModule : ImpliciXModule
  {
    public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
      => new ThingsBoardModule(moduleName, rtDef.Application.AppName, rtDef.Module<ThingsBoardModuleDefinition>());

    public ThingsBoardModule(string id, string appName, ThingsBoardModuleDefinition moduleDefinition) : base(id)
    {
      DefineModule(
        initDependencies: cfg => cfg.AddSettings<ThingsBoardSettings>("Modules", Id),
        initResources: provider =>
        {
          var clock = provider.GetService<IClock>();
          var settings = provider.GetSettings<ThingsBoardSettings>(Id);
          var elementsQueue = new BoundedQueue<IThingsBoardMessage>(settings.MessageQueueMaxCapacity);
          var context = new Context(appName, settings);
          var publishers = new Publisher[]
          {
            new Telemetry(moduleDefinition.Telemetry, elementsQueue)
          };

          var gatherConfiguration = new GatherConfiguration(moduleDefinition);
          var waitBeforeRetryConnect = new WaitBeforeRetryConnect(clock, moduleDefinition.RetryDelay, context.RetryContext);
          var disabled = new Disabled(clock, moduleDefinition.EnableDelay);
          var connectToBroker = new ConnectToBroker(clock);
          var sendMessages = new SendMessages(clock, elementsQueue, context);
          var states = new[]
          {
            gatherConfiguration.Define(),
            waitBeforeRetryConnect.Define(),
            disabled.Define(),
            connectToBroker.Define(),
            sendMessages.Define()
          };
          var transitions = new[]
          {
            gatherConfiguration.WhenGatheringIsComplete(connectToBroker),
            connectToBroker.WhenConnectionIsSuccess(sendMessages),
            connectToBroker.WhenConnectionIsFailed(waitBeforeRetryConnect),
            waitBeforeRetryConnect.WhenConnectionIsDisabled(disabled),
            waitBeforeRetryConnect.WhenTimeoutOccured(connectToBroker),
            sendMessages.WhenConnectionIsFailed(connectToBroker),
            disabled.WhenTimeoutOccured(connectToBroker)
          };

          var runner = new Runner(context, gatherConfiguration, states, transitions);
          runner.Activate();

          return new object[]
          {
            clock,
            context,
            runner,
            elementsQueue,
            publishers
          };
        },
        createFeature: assets =>
        {
          var runner = assets.Get<Runner>();
          var publishers = assets.Get<Publisher[]>();
          return DefineFeature()
            .Handles<PropertiesChanged>(pc => HandlePropertiesChanged(pc, runner, publishers))
            .Handles<GatherConfiguration.GatheringComplete>(runner.Handle, runner.CanHandle)
            .Handles<ConnectToBroker.ConnectionSuccess>(runner.Handle, runner.CanHandle)
            .Handles<ConnectToBroker.ConnectionFailed>(runner.Handle, runner.CanHandle)
            .Handles<WaitBeforeRetry.ConnectionDisabled>(runner.Handle, runner.CanHandle)
            .Handles<TimeoutOccured>(runner.Handle, runner.CanHandle)
            .Handles<SystemTicked>(st => HandleSystemTicked(st, runner, publishers))
            .Handles<Disabled.Enabled>(runner.Handle, runner.CanHandle)
            .Create();
        });
    }

    private DomainEvent[] HandlePropertiesChanged(PropertiesChanged propertiesChanged, Runner runner,
      Publisher[] publishers)
    {
      foreach (var publisher in publishers)
        publisher.Handles(propertiesChanged);
      return runner.CanHandle(propertiesChanged) ? runner.Handle(propertiesChanged) : Array.Empty<DomainEvent>();
    }

    private DomainEvent[] HandleSystemTicked(SystemTicked ticked, Runner runner, Publisher[] publishers)
    {
      foreach (var publisher in publishers)
        publisher.Handles(ticked);
      return runner.CanHandle(ticked) ? runner.Handle(ticked) : Array.Empty<DomainEvent>();
    }
  }
}