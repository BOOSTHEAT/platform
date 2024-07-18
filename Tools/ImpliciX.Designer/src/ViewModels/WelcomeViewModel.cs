using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ImpliciX.Designer.Features;
using ImpliciX.DesktopServices;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public class WelcomeViewModel : NamedModel, IDisposable
{
  private readonly IFeatures _features;
  private readonly CompositeDisposable _subscriptions;
  private IEnumerable<SessionCommands> _deviceDefinitions;

  public WelcomeViewModel(
    IFeatures features
  ) : base("Welcome")
  {
    _features = features;

    var dds = features.Concierge.Session.HistoryUpdates
      .Select(x => ToOpenDeviceDefinition(x.ToArray()))
      .BindTo(
        this,
        x => x.DeviceDefinitionPaths
      );
    DeviceDefinitionPaths = ToOpenDeviceDefinition(features.Concierge.Session.History.ToArray());

    _subscriptions = new CompositeDisposable(dds);
  }

  public IMainWindow Window => _features.Window;

  public IEnumerable<SessionCommands> DeviceDefinitionPaths
  {
    get => _deviceDefinitions;
    private set => this.RaiseAndSetIfChanged(
      ref _deviceDefinitions,
      value
    );
  }

  protected ISessionService.Session[] Sessions
  {
    set => DeviceDefinitionPaths = ToOpenDeviceDefinition(value);
  }

  public void Dispose()
  {
    _subscriptions.Dispose();
  }

  private IEnumerable<SessionCommands> ToOpenDeviceDefinition(
    ISessionService.Session[] sessions
  )
  {
    return sessions
      .Where(session => !string.IsNullOrEmpty(session.Connection) || !string.IsNullOrEmpty(session.Path))
      .Select(
        session => new SessionCommands(
          session,
          _features
        )
      )
      .ToArray();
  }
}
