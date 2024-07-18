using System;
using ImpliciX.Harmony.Infrastructure;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Publishers;
using ImpliciX.Harmony.States;
using ImpliciX.Language;
using ImpliciX.RuntimeFoundations;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;
using ImpliciX.SharedKernel.Modules;
using static ImpliciX.SharedKernel.Modules.ImpliciXFeatureDefinition;
using Metrics = ImpliciX.Harmony.Publishers.Metrics;

namespace ImpliciX.Harmony
{
  public class HarmonyModule : ImpliciXModule
  {
    public static IImpliciXModule Create(string moduleName, ApplicationRuntimeDefinition rtDef)
      => new HarmonyModule(moduleName, rtDef.Application.AppName, rtDef.Module<HarmonyModuleDefinition>());

    public HarmonyModule(string id, string appName, HarmonyModuleDefinition moduleDefinition) : base(id)
    {
      DefineModule(
        initDependencies: cfg => cfg.AddSettings<HarmonySettings>("Modules", Id),
        initResources: provider =>
        {
          var clock = provider.GetService<IClock>();
          var harmonySettings = provider.GetSettings<HarmonySettings>(Id);
          var elementsQueue = new BoundedQueue<IHarmonyMessage>(harmonySettings.MessageQueueMaxCapacity);
          var context = new Context(
            appName,
            harmonySettings.DpsUri,
            harmonySettings.GlobalRetries,
            TimeSpan.FromMilliseconds(harmonySettings.RegistrationTimeout));
          var publishers = new Publisher[]
          {
            new Alarms(moduleDefinition.AlarmCodeFromAlarmStateUrn, elementsQueue),
            new Metrics(elementsQueue),
            new AdditionalID(moduleDefinition.AdditionalId, elementsQueue),
            new LiveData(moduleDefinition.LiveData, elementsQueue)
          };

          var gatherConfiguration = new GatherConfiguration(moduleDefinition);
          var enrollWithDps = new EnrollWithDps(clock, AzureIotHubAdapter.RegisterWithDps);
          var waitBeforeRetryEnrollment =
            new WaitBeforeRetryEnrollment(clock, moduleDefinition.RetryDelay, context.DpsRetryContext);
          var waitBeforeRetryConnect = new WaitBeforeRetryConnect(clock, moduleDefinition.RetryDelay, context.IotHubRetryContext);
          var disabled = new Disabled(clock, moduleDefinition.EnableDelay);
          var connectToIotHub = new ConnectToIotHub(clock);
          var sendMessages = new SendMessages(clock, elementsQueue, context);
          var states = new[]
          {
            gatherConfiguration.Define(),
            enrollWithDps.Define(),
            waitBeforeRetryEnrollment.Define(),
            waitBeforeRetryConnect.Define(),
            disabled.Define(),
            connectToIotHub.Define(),
            sendMessages.Define()
          };
          var transitions = new[]
          {
            gatherConfiguration.WhenGatheringIsComplete(enrollWithDps),
            enrollWithDps.WhenEnrollmentIsSuccess(connectToIotHub),
            enrollWithDps.WhenEnrollmentFailed(waitBeforeRetryEnrollment),
            waitBeforeRetryEnrollment.WhenConnectionIsDisabled(disabled),
            waitBeforeRetryEnrollment.WhenTimeoutOccured(enrollWithDps),
            connectToIotHub.WhenConnectionIsSuccess(sendMessages),
            connectToIotHub.WhenConnectionIsFailed(waitBeforeRetryConnect),
            waitBeforeRetryConnect.WhenConnectionIsDisabled(disabled),
            waitBeforeRetryConnect.WhenTimeoutOccured(connectToIotHub),
            sendMessages.WhenConnectionIsFailed(connectToIotHub),
            disabled.WhenTimeoutOccured(enrollWithDps)
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
            .Handles<EnrollWithDps.EnrollmentSuccess>(runner.Handle, runner.CanHandle)
            .Handles<EnrollWithDps.EnrollmentFailed>(runner.Handle, runner.CanHandle)
            .Handles<ConnectToIotHub.ConnectionSuccess>(runner.Handle, runner.CanHandle)
            .Handles<ConnectToIotHub.ConnectionFailed>(runner.Handle, runner.CanHandle)
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