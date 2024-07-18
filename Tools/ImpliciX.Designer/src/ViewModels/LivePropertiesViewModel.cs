using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ReactiveUI;
using static ImpliciX.Designer.ViewModels.LivePropertyViewModel;

namespace ImpliciX.Designer.ViewModels;

public class LivePropertiesViewModel : DockableViewModel, IDisposable
{
  private readonly ReadOnlyObservableCollection<LivePropertyViewModel> _items;
  private readonly Func<string, Task<bool>> _sendTarget;
  private readonly SourceCache<ImpliciXProperty, string> _sessionProperties;

  private readonly IDisposable _subscription;

  private Func<string, bool> _contextFilter;

  private IEnumerable<string> _contextUrns;

  private bool _isConnected;

  private string _search;

  public LivePropertiesViewModel(
    ILightConcierge concierge
  ) : this(
    concierge.Applications.Devices,
    concierge.Session.Properties,
    concierge.RemoteDevice.Send
  )
  {

    concierge.RemoteDevice.IsConnected
      .Subscribe(isConnected => IsConnected = isConnected);
  }


  protected LivePropertiesViewModel(
    IObservable<Option<IDeviceDefinition>> applicationsDevices,
    SourceCache<ImpliciXProperty, string> sessionProperties,
    Func<string, Task<bool>> send
  )

  {
    Title = "Properties";
    CanClose = false;
    _sendTarget = send;
    _sessionProperties = sessionProperties;
    _search = string.Empty;
    _contextUrns = Enumerable.Empty<string>();

    var propertyInfos = new SourceCache<PropertyInfo, string>(x => x.Name);

    var modelUrns = applicationsDevices
      .Prepend(Option<IDeviceDefinition>.None())
      .Select(
        odd => odd.Match(
          () => new Dictionary<string, Urn>(),
          dd => dd.Urns
        )
      )
      .ToObservableChangeSet()
      .AddKey(odd => 0);

    var remoteUrns = sessionProperties
      .Connect()
      .DistinctValues(x => x.Urn);

    var urnsSubscription = modelUrns
      .FullJoinMany(
        remoteUrns,
        _ => 0,
        (
          dd,
          grp
        ) => dd.Value.Keys
          .Concat(grp.Keys)
          .Distinct()
          .ToDictionary(
            name => name,
            name => new PropertyInfo(
              name,
              dd.Value.TryGetValue(
                name,
                out var u
              )
                ? u
                : Option<Urn>.None()
            )
          )
      )
      .RemoveKey()
      .OnItemAdded(
        props => propertyInfos.Edit(
          upd =>
          {
            upd.Remove(upd.Keys.Except(props.Keys));
            upd.AddOrUpdate(props);
          }
        )
      )
      .Subscribe();

    var contextFilterSubscription = this
      .WhenValueChanged(@this => @this.ContextURNs)
      .Select(
        urns =>
        {
          var hs = urns.ToHashSet();

          bool CurrentContextFilter(
            string urn
          )
          {
            return hs.Count == 0 || hs.Contains(urn);
          }

          return (Func<string, bool>)CurrentContextFilter;
        }
      ).BindTo(
        this,
        x => x.ContextFilter
      );

    Func<PropertyInfo, bool> UserSearchFilter(
      string search
    )
    {
      return x => x.Name.Contains(search);
    }

    var urnFilter = this
      .WhenAnyValue(
        @this => @this.ContextFilter,
        @this => @this.Search,
        (
          contextFilter,
          search
        ) => search.Length == 0
          ? pc => contextFilter(pc.Name)
          : UserSearchFilter(search)
      );

    var itemsSubscription = propertyInfos
      .Connect()
      .Filter(urnFilter)
      .Select(
        pi => CreateLivePropertyViewModel(
          pi,
          this
        )
      )
      .SortBy(x => x.Urn.Value)
      .ObserveOn(RxApp.MainThreadScheduler)
      .Bind(out _items)
      .Subscribe();

    var compositeDisposable = new CompositeDisposable(
      urnsSubscription,
      contextFilterSubscription,
      itemsSubscription,
      propertyInfos
    );

    _subscription = compositeDisposable;
  }

  public ReadOnlyObservableCollection<LivePropertyViewModel> Items => _items;

  public string Search
  {
    get => _search;
    set => this.RaiseAndSetIfChanged(
      ref _search,
      value
    );
  }

  public IEnumerable<string> ContextURNs
  {
    get => _contextUrns;
    set => this.RaiseAndSetIfChanged(
      ref _contextUrns,
      value
    );
  }

  public bool IsConnected
  {
    get => _isConnected;
    set => this.RaiseAndSetIfChanged(
      ref _isConnected,
      value
    );
  }

  public Func<string, bool> ContextFilter
  {
    get => _contextFilter;
    set => this.RaiseAndSetIfChanged(
      ref _contextFilter,
      value
    );
  }

  public void Dispose()
  {
    _subscription.Dispose();
  }

  public void EraseSearch()
  {
    Search = string.Empty;
  }

  public void SendToApp(
    string message
  )
  {
    _sendTarget(message);
  }

  public IObservable<Change<ImpliciXProperty, string>> WatchUrn(
    string urn
  )
  {
    return _sessionProperties.Watch(urn);
  }

  public record PropertyInfo(string Name, Option<Urn> Definition)
  {
    public bool IsInModel => Definition.IsSome;
  }
}
