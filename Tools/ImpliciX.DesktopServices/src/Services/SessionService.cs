using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData;
using ImpliciX.Data.Factory;
using ImpliciX.DesktopServices.Helpers;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using JetBrains.Annotations;

namespace ImpliciX.DesktopServices.Services;

internal class SessionService : ISessionService
{
  private readonly CompositeDisposable _disposables;

  public SessionService([NotNull] IRemoteDevice remoteDevice, [NotNull] IObservable<Option<IDeviceDefinition>> devices)
  {
    if (devices == null) throw new ArgumentNullException(nameof(devices));

    Properties = new SourceCache<ImpliciXProperty, string>(x => x.Urn);

    DefinitionTools definitionTools = null;

    var deviceDefinition = devices
      .Subscribe(optionalDef =>
      {
        Properties.Edit(updater =>
        {
          optionalDef.Tap(
            updater.Clear,
            def =>
            {
              definitionTools = DefinitionTools.Create(def);
              foreach (var urn in def.Urns.Keys)
              {
                var existingProperty = Properties.Lookup(urn);
                if (existingProperty.HasValue)
                {
                  TransformProperty(existingProperty.Value, definitionTools).Tap(updater.AddOrUpdate);
                }
              }
            }
          );
        });
      });

    var remoteDeviceProperties = remoteDevice.Properties
      .Connect()
      .Subscribe(properties =>
      {
        Properties.Edit(updater =>
        {
          properties
            .Where(change => change.Reason is ChangeReason.Add or ChangeReason.Update)
            .ForEach(change => TransformProperty(change.Current, definitionTools).Tap(updater.AddOrUpdate));

          properties
            .Where(change => change.Reason is ChangeReason.Remove)
            .ForEach(change => updater.Remove(change.Current));
        });
      });

    Updates = devices.Prepend(Option<IDeviceDefinition>.None())
      .CombineLatest(Optionalize(remoteDevice.TargetSystem), Optionalize(remoteDevice.DeviceDefinition))
      .Select(SessionDetails.CreateFrom)
      .Where(s => s.IsWorthy)
      .Select(s => s.ToSession());

    Updates.Subscribe(s =>
    {
      Current = s;
      _history.Record(s);
    });

    _disposables = new CompositeDisposable(
      new[] { remoteDeviceProperties, deviceDefinition }
        .Where(it => it != null));
  }

  private IObservable<Option<T>> Optionalize<T>(IObservable<T> obs)
    => obs.Select(arg => arg == null ? Option<T>.None() : Option<T>.Some(arg)).Prepend(Option<T>.None());

  private static Result<ImpliciXProperty> TransformProperty([NotNull] ImpliciXProperty property,
    [CanBeNull] DefinitionTools definitionTools = null)
  {
    if (definitionTools == null) return property;

    var (modelFactory, stateUrnsTypes) = definitionTools;
    if (stateUrnsTypes.TryGetValue(property.Urn, out var stateType))
    {
      if (Int32.TryParse(property.Value, out var value))
      {
        return property with { Value = Enum.ToObject(stateType, value).ToString() };
      }

      if (Enum.TryParse(stateType, property.Value, out var enumValue))
      {
        return property with { Value = enumValue.ToString() };
      }
    }

    return modelFactory.FindUrnType(property.Urn).Match(
      _ => property,
      _ => modelFactory.Create(property.Urn, property.Value)
        .Map(value => property with { Value = ((IDataModelValue)value).ModelValue().ToString() })
    );
  }

  public ISessionService.Session Current { get; set; }
  public IObservable<ISessionService.Session> Updates { get; }
  public IEnumerable<ISessionService.Session> History => _history;
  public IObservable<IEnumerable<ISessionService.Session>> HistoryUpdates => _history.Subject;
  private readonly History<ISessionService.Session> _history = new(SessionHistorySize, PersistenceKey);
  private static readonly int SessionHistorySize = int.Parse(Environment.GetEnvironmentVariable("IMPLICIX_SESSION_HISTORY_SIZE") ?? "10");
  internal const string PersistenceKey = "SessionHistory";
  public SourceCache<ImpliciXProperty, string> Properties { get; }

  public void Dispose()
  {
    _disposables?.Dispose();
    Properties?.Dispose();
  }
}

internal record SessionDetails(
  string DeviceDefinitionPath,
  SessionDetails.AppIdentity Local,
  string ConnectionString,
  SessionDetails.AppIdentity Remote)
{
  public bool IsWorthy => !Empty && MatchingAppNames;
  
  private bool Empty =>
    string.IsNullOrEmpty(DeviceDefinitionPath)
    && string.IsNullOrEmpty(ConnectionString);

  private bool MatchingAppNames =>
    string.IsNullOrEmpty(Local.Name)
    || string.IsNullOrEmpty(Remote.Name)
    || Local.Name == Remote.Name;

  internal record AppIdentity(string Name, string Version);

  public static SessionDetails CreateFrom((
    Option<IDeviceDefinition> DeviceDefinition,
    Option<ITargetSystem> TargetSystem,
    Option<IRemoteDeviceDefinition> RemoteDeviceDefinition) data)
  {
    string ValueOrEmpty<T>(Option<T> x, Func<T, string> val) => x.Match(() => "", val);
    return new SessionDetails(
      ValueOrEmpty(data.DeviceDefinition, x => x.Path),
      new AppIdentity(
        ValueOrEmpty(data.DeviceDefinition, x => x.Name),
        ValueOrEmpty(data.DeviceDefinition, x => x.Version)),
      ValueOrEmpty(data.TargetSystem, x => x.ConnectionString),
      new AppIdentity(ValueOrEmpty(data.RemoteDeviceDefinition, x => x.Name),
        ValueOrEmpty(data.RemoteDeviceDefinition, x => x.Version))
    );
  }

  public ISessionService.Session ToSession()
  {
    return new ISessionService.Session(DeviceDefinitionPath, ConnectionString);
  }
}

internal record DefinitionTools(ModelFactory ModelFactory, Dictionary<string, Type> StateUrnsTypes)
{
  [CanBeNull]
  public static DefinitionTools Create(Option<IDeviceDefinition> def)
  {
    if (def.IsNone) return null;

    var deviceDefinition = def.GetValue();
    return Create(deviceDefinition);
  }

  public static DefinitionTools Create(IDeviceDefinition deviceDefinition)
  {
    var modelFactory = deviceDefinition.ModelFactory;
    Dictionary<string, Type> stateUrnsTypes = deviceDefinition.SubSystemDefinitions
      .ToDictionary(it => it.StateUrn.Value, it => it.StateType);

    return new DefinitionTools(modelFactory, stateUrnsTypes);
  }
};