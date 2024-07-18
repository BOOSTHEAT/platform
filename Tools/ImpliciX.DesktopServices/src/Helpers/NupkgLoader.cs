using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ImpliciX.Language;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using RestSharp.Extensions;

namespace ImpliciX.DesktopServices.Helpers;

internal class NupkgLoader
{
  public static (string NuPkgId, ApplicationDefinition App) CreateApplication(string nupkgPath)
  {
    var result = LoadAssemblies(nupkgPath);

    var appDefFactory = new ApplicationDefinitionFactory();
    return (
      result.NuPkgId,
      appDefFactory.CreateEntryPointFrom(result.Assemblies.SelectMany(a => a.GetTypes()).ToArray(), nupkgPath)
      );
  }

  public static (string NuPkgId, IEnumerable<Assembly> Assemblies) LoadAssemblies(string nupkgPath)
  {
    using var packageReader = new PackageArchiveReader(nupkgPath);
    var libsPaths = packageReader.GetLibItems().SelectMany(x => x.Items).ToArray();
    if (libsPaths.IsEmpty())
      throw new FileNotFoundException($"No assembly found in {nupkgPath}");

    var assemblies = LoadAssemblies(packageReader, libsPaths);
    var result = (NuPkgId: packageReader.GetIdentity().Id, Assemblies: assemblies);
    return result;
  }

  private static IEnumerable<Assembly> LoadAssemblies(IPackageCoreReader reader, string[] libsPaths)
  {
    var assemblies = new ConcurrentDictionary<string, Assembly>();

    AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
    {
      var assemblyName = new AssemblyName(args.Name!);
      return assemblies.GetValueOrDefault(assemblyName.Name);
    };

    foreach (var libsPath in libsPaths)
    {
      using var stream = reader.GetStream(libsPath);
      var bytes = stream.ReadAsBytes();
      var assembly = Assembly.Load(bytes);
      assemblies.TryAdd(assembly.GetName().Name, assembly);
    }

    var assembliesValues = assemblies.Values;
    return assembliesValues;
  }
}