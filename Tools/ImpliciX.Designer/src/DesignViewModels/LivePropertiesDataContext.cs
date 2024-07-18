using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using ImpliciX.Data.Factory;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.DesignViewModels;

public class LivePropertiesDataContext : LivePropertiesViewModel
{
  private static readonly string ContextTitle = "DataContext";

  private static readonly ILightConcierge
    Concierge = IConcierge.Create(new User(ContextTitle)); // new LightConcierge();

  internal static readonly SourceCache<ImpliciXProperty, string> SessionProperties = new (x => x.Urn);
  private static readonly Subject<Option<IDeviceDefinition>> Devices = new ();

  private readonly Urn _rootUrn = Urn.BuildUrn(
    "test",
    "very",
    "long",
    "url",
    "to",
    "check",
    "alignment",
    "issue"
  );

  public LivePropertiesDataContext(
  ) : base(
    ApplicationsDevices,
    SessionProperties,
    Send
  )
  {
    LivePropertiesViewModel parent = this;

    var root = LivePropertyViewModel.CreateLivePropertyViewModel(
      new PropertyInfo(
        "rootUrn",
        _rootUrn
      ),
      parent
    );

    IEnumerable<ISubSystemDefinition> subSystemDefinitions = Array.Empty<ISubSystemDefinition>();
    IDictionary<string, Urn> urns = new Dictionary<string, Urn>();
    urns[_rootUrn] = _rootUrn;

    NewEntry<Percentage>(
      urns,
      "Progress",
      "0.5"
    );

    NewEntry<UpdateState>(
      urns,
      "Status",
      "Starting"
    );

    NewEntry<Mass>(
      urns,
      "Weight",
      "68"
    );

    NewEntry<Energy>(
      urns,
      "Power",
      null
    );

    NewEntry<DifferentialTemperature>(
      urns,
      "Heating",
      "-2"
    );

    NewEntry<DifferentialPressure>(
      urns,
      "Compression",
      "3"
    );

    NewEntry<Guid>(
      urns,
      "Guid",
      "test GUID"
    );

    NewEntry<RotationalSpeed>(
      urns,
      "Speed",
      "1500"
    );

    NewEntry<AngularSpeed>(
      urns,
      "Moving",
      "50"
    );

    NewEntry<FunctionDefinition>(
      urns,
      "Function",
      "Kd:1"
    );

    NewEntry<FunctionDefinition>(
      urns,
      "Function2",
      "a10:1|a01:1.1"
    );

    Option<IDeviceDefinition> device = new DeviceDefinition { Urns = urns };
    Devices.OnNext(device);
    IsConnected = true;
  }

  private static IObservable<Option<IDeviceDefinition>> ApplicationsDevices => Devices;

  private void NewEntry<type>(
    IDictionary<string, Urn> urns,
    string name,
    string value
  )
  {
    var entryUrn =
      UserSettingUrn<type>.Build(
        _rootUrn,
        name
      );

    SessionProperties.AddOrUpdate(
      new ImpliciXProperty(
        entryUrn,
        value
      )
    );
    urns[entryUrn] = entryUrn;
  }

  private static Task<bool> Send(
    string name
  )
  {
    return Task.FromResult(true);
  }
}

public record DeviceDefinition  : IDeviceDefinition
{
  public string Path { get; init; } = null;
  public string Name { get; init; } = null;
  public string Version { get; init; } = null;
  public string EntryPoint { get; init; } = null;
  public ModelFactory ModelFactory { get; init; } = null;
  public MetricsModuleDefinition Metrics { get; init; } = null;
  public UserInterfaceModuleDefinition UserInterface { get; init; } = null;
  public IEnumerable<ISubSystemDefinition> SubSystemDefinitions { get; init; }  = Array.Empty<ISubSystemDefinition>();
  public IDictionary<string, Urn> Urns { get; init; } = new Dictionary<string, Urn>();
  public IEnumerable<Urn> UserSettings { get; init; }  = Array.Empty<Urn>();
  public IEnumerable<Urn> VersionSettings { get; init; }  = Array.Empty<Urn>();
  public IEnumerable<Urn> AllSettings { get; init; }  = Array.Empty<Urn>();
}
