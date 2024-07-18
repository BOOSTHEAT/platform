using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData.Binding;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Metrics.Internals;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public class SystemViewModel : DockableViewModel, IDisposable
{

  public SystemViewModel(ILightConcierge concierge, NamedTree[] defaultTree,
    Func<ISubSystemDefinition, SubSystemViewModel> subsystemViewModelFactory)
  {
    CanClose = false;
    SelectModel = new Subject<NamedModel>();
    concierge?.Applications?.Devices?.Subscribe(
      odd => Device = odd.Match(
        () => null,
        dd => dd
        ));
    _subscription = new CompositeDisposable(
      this
        .WhenValueChanged(@this => @this.Device)
        .Select(device =>
        {
          return device == null
            ? null
            : LoadModelItems(device.SubSystemDefinitions.ToArray(), def => def.ID.Value, subsystemViewModelFactory);
        }).BindTo(this, x => x.PotentialSubSystems),
      this
        .WhenValueChanged(@this => @this.PotentialSubSystems)
        .Select(psss =>
        {
          return psss
            ?.Where(x => x.Value.IsSuccess)
            ?.ToDictionary(x => x.Key, x => x.Value.Value);
        }).BindTo(this, x => x.SubSystems),
      this
        .WhenAnyValue(@this => @this.Device, @this => @this.PotentialSubSystems)
        .Select(v =>
        {
          var device = v.Item1;
          var subSystemViewModels = v.Item2;
          if (device == null || subSystemViewModels == null)
            return defaultTree;
          return CreateSystemTree(device, subSystemViewModels);
        }).BindTo(this, x => x.Models),
      this
        .WhenAnyValue(@this => @this.Device)
        .Select(device => $"{device?.Name} {device?.Version}")
        .BindTo(this, x => x.Title));
    this
      .WhenValueChanged(@this => this.Models)
      .Subscribe(tree =>
      {
        Select(tree.First().Parent);
      });
    
  }

  private static NamedTree[] CreateSystemTree(IDeviceDefinition device,
    IReadOnlyDictionary<string, Result<SubSystemViewModel>> subSystemViewModels)
  {
    var metrics = device.Metrics == null
      ? Enumerable.Empty<MetricViewModel>()
      : device.Metrics?.Metrics.Select(def => new MetricViewModel(def.Builder.Build<IMetric>())).ToArray();
    var metricsSubTree = new NamedTree(
      new MetricsModuleViewModel(device.Metrics),
      metrics
    );
    var controlCommandSubTree = new NamedTree(
      new ControlCommandModuleViewModel(device.SubSystemDefinitions),
      subSystemViewModels.Values.Select(r => r.IsSuccess ? r.Value : new NamedModel(r.Error.Message))
    );
    return new NamedTree[]
    {
      new NamedTree(
        new ApplicationDefinitionViewModel(device),
        controlCommandSubTree,
        metricsSubTree
      )
    };
  }

  public void Select(NamedModel nm) => SelectModel.OnNext(nm);

  public readonly Subject<NamedModel> SelectModel;

  private IReadOnlyDictionary<string, Result<V>> LoadModelItems<U, V>(U[] input, Func<U, string> id, Func<U, V> make)
  {
    var duplicates = input
      .GroupBy(x => id(x))
      .Where(x => x.Count() > 1)
      .Select(x => x.Key).ToHashSet();
    return input
      .Where(ssvm => !duplicates.Contains(id(ssvm)))
      .Select(def =>
        {
          try
          {
            return (id(def), make(def));
          }
          catch (Exception e)
          {
            return (id(def), (Result<V>)new Error(id(def), e.Message));
          }
        }
      ).Concat(duplicates
        .Select(x => (x, (Result<V>)new Error(x, "ERROR duplicate definitions")))
      )
      .ToDictionary(x => x.Item1, x => x.Item2);
  }

  public IDeviceDefinition Device
  {
    get => _device;
    set { this.RaiseAndSetIfChanged(ref _device, value); }
  }

  private IDeviceDefinition _device;

  public IEnumerable<SubSystemViewModel> OrderedSubSystems => SubSystems.Values.OrderBy(ss => ss.Name);

  public IReadOnlyDictionary<string, SubSystemViewModel> SubSystems
  {
    get => _subSystems;
    set { this.RaiseAndSetIfChanged(ref _subSystems, value); }
  }

  private IReadOnlyDictionary<string, SubSystemViewModel> _subSystems;

  public IReadOnlyDictionary<string, Result<SubSystemViewModel>> PotentialSubSystems
  {
    get => _potentialSubSystems;
    set { this.RaiseAndSetIfChanged(ref _potentialSubSystems, value); }
  }

  private IReadOnlyDictionary<string, Result<SubSystemViewModel>> _potentialSubSystems;

  public IEnumerable<NamedTree> Models
  {
    get => _models;
    set { this.RaiseAndSetIfChanged(ref _models, value); }
  }

  private IEnumerable<NamedTree> _models;

  public void Dispose()
  {
    _subscription.Dispose();
  }

  private IDisposable _subscription;
}