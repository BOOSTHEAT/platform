using System;
using System.Linq;
using ImpliciX.Language;

namespace ImpliciX.DesktopServices.Helpers;

internal interface IApplicationDefinitionFactory
{
  ApplicationDefinition CreateEntryPointFrom(Type[] assemblyTypes, string sourcePath);
}

internal sealed class ApplicationDefinitionFactory : IApplicationDefinitionFactory
{
  public ApplicationDefinition CreateEntryPointFrom(Type[] types, string source)
  {
    var candidates = types
      .Where(t => t.IsClass && !t.IsAbstract && t.BaseType?.FullName == typeof(ApplicationDefinition).FullName)
      .ToArray();

    switch (candidates.Length)
    {
      case 0: throw new ApplicationException($"Cannot find application entry point in {source}");
      case > 1: throw new ApplicationException($"Multiple entry points in {source}\n{string.Join('\n', candidates.Select(c => c.FullName))}");
    }

    var mainType = candidates.First();
    var currentLanguage = typeof(ApplicationDefinition).Assembly.GetName();
    var appLanguage = mainType.Assembly.GetReferencedAssemblies()
      .FirstOrDefault(a => a.Name == typeof(ApplicationDefinition).Assembly.GetName().Name);

    try
    {
      var appMain = (ApplicationDefinition) Activator.CreateInstance(mainType!)!;
      return appMain;
    }
    catch (Exception e)
    {
      throw new ApplicationException($"Cannot load application {source}"
                                     + $"\nApplication language: {appLanguage?.Version}"
                                     + $"\nSupported language: {currentLanguage.Version}", e);
    }
  }
}