using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.JavaScript;
using ImpliciX.Data.Factory;
using ImpliciX.Language;
using ImpliciX.Language.Control;
using ImpliciX.Language.Metrics;
using ImpliciX.Language.Metrics.Internals;
using ImpliciX.Runtime;

namespace ImpliciX.RuntimeFoundations
{
  public class ApplicationRuntimeDefinition
  {
    public ApplicationRuntimeDefinition(ApplicationDefinition applicationDefinition,
      ApplicationOptions applicationOptions, string[] setups)
    {
      Application = applicationDefinition;
      var dmd = applicationDefinition.DataModelDefinition;
      ModelDefinition = dmd?.Assembly;
      ModelFactory = new ModelFactory(dmd?.Assembly, dmd?.ModelBackwardCompatibility);
      Options = applicationOptions;
      Setups = setups;
      Metrics = Module<MetricsModuleDefinition>()?.Metrics?.Select(def => def.Builder.Build<IMetric>()).ToArray();
      StateMachines = FindStateMachines(Module<ControlModuleDefinition>()?.Assembly) ?? Array.Empty<ISubSystemDefinition>();
    }
    public readonly ApplicationDefinition Application;
    public readonly ApplicationOptions Options;
    public readonly Assembly ModelDefinition;
    public readonly ModelFactory ModelFactory;
    public readonly IMetric[] Metrics;
    public readonly ISubSystemDefinition[] StateMachines;
    public readonly string[] Setups;
    public T Module<T>() => (T)Application.ModuleDefinitions?.FirstOrDefault(m => m is T);

    public static ISubSystemDefinition[] FindStateMachines(Assembly definitionAssembly) => definitionAssembly
      ?.GetTypes()
      .Where(t => IsSubsystemDefinition(t) && !IsFragmentDefinition(t))
      .Select(Activator.CreateInstance)
      .Cast<ISubSystemDefinition>()
      .ToArray();
    
    private static bool IsSubsystemDefinition(Type t) =>
      t.IsClass && 
      t.IsPublic && 
      !t.IsGenericType && 
      t.GetConstructor(Type.EmptyTypes) != null && 
      typeof(ISubSystemDefinition).IsAssignableFrom(t);

    private static bool IsFragmentDefinition(Type t) => typeof(IFragmentDefinition).IsAssignableFrom(t);
  }
}