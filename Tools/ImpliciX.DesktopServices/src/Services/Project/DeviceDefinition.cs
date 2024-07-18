using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Control;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Model;

namespace ImpliciX.DesktopServices.Services.Project;

internal class DeviceDefinition : IDeviceDefinition
{
  public DeviceDefinition(string path, ApplicationDefinition main)
  {
    Path = path;
    _main = main;
    Name = main.AppName;
    Version = main.GetType().Assembly.GetName().Version?.ToString();
    Model = _main.DataModelDefinition.Assembly;
    UserInterface = M<UserInterfaceModuleDefinition>(_main);
    Metrics = M<MetricsModuleDefinition>(_main);
    Program = M<ControlModuleDefinition>(_main)?.Assembly;
    SubSystemDefinitions = Definitions(Program) ?? Enumerable.Empty<ISubSystemDefinition>();
    ModelFactory = new ModelFactory(Model);
    Urns = ModelFactory.GetAllUrns().ToDictionary(u => u.Value, u => u);
    UserSettings = Urns.Values.Where(IsSetting(typeof(UserSettingUrn<>))).ToArray();
    VersionSettings = Urns.Values.Where(IsSetting(typeof(VersionSettingUrn<>))).ToArray();
    AllSettings = UserSettings.Concat(VersionSettings).ToArray();
  }

  public static T M<T>(ApplicationDefinition app) => (T) app.ModuleDefinitions.FirstOrDefault(m => m is T);

  private static Func<Urn, bool> IsSetting(Type refType) =>
    urn =>
    {
      var type = urn.GetType();
      return type.IsGenericType && type.GetGenericTypeDefinition() == refType;
    };

  public string EntryPoint => _main.GetType().FullName;
  public object[] MainAppModuleDefinitions => _main.ModuleDefinitions;

  public string Path { get; }
  public string Name { get; }
  public string Version { get; }
  public Assembly Model { get; }
  public Assembly Program { get; }
  public UserInterfaceModuleDefinition UserInterface { get; }
  public MetricsModuleDefinition Metrics { get; }
  public IEnumerable<ISubSystemDefinition> SubSystemDefinitions { get; }

  public ModelFactory ModelFactory { get; }
  public IDictionary<string, Urn> Urns { get; }
  public IEnumerable<Urn> UserSettings { get; }
  public IEnumerable<Urn> VersionSettings { get; }
  public IEnumerable<Urn> AllSettings { get; }

  private readonly ApplicationDefinition _main;

  private static bool IsSubsystemDefinition(Type t)
  {
    return t.IsClass &&
           t.IsPublic &&
           !t.IsGenericType &&
           t.GetConstructor(Type.EmptyTypes) != null &&
           typeof(ISubSystemDefinition).IsAssignableFrom(t);
  }
  
  private static bool IsValidSubSystemDefinition(ISubSystemDefinition ssd)
  {
    try
    {
      return ssd.StateUrn.Value.Length > 0 && ssd.StateType.FullName!.Length > 0;
    }
    catch (Exception)
    {
      return false;
    }
  }


  private static IEnumerable<ISubSystemDefinition> Definitions(Assembly definitionAssembly) => definitionAssembly
    ?.GetTypes()
    .Where(IsSubsystemDefinition)
    .Select(Activator.CreateInstance)
    .Cast<ISubSystemDefinition>()
    .Where(IsValidSubSystemDefinition);
}