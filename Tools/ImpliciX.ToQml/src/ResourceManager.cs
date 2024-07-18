using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace ImpliciX.ToQml;

public class ResourceManager
{
    public static Assembly StandardResources = Assembly.GetExecutingAssembly();

    public static string[] Load(DirectoryInfo output, Assembly assetsAssembly, ICopyrightManager copyrightManager)
    {
        var languageAssembly = Assembly.GetExecutingAssembly();
        var resourceGroups = new (string Name, Assembly Assembly)[]
        {
            (languageAssembly.GetName().Name + ".Qml", languageAssembly),
            (assetsAssembly.GetName().Name, assetsAssembly)
        };

        var resources =
        (
            from resourceGroup in resourceGroups
            from resourceName in resourceGroup.Assembly.GetManifestResourceNames()
            select ExtractFile(output, resourceGroup, resourceName, copyrightManager)
        ).ToArray();

        return resources;
    }

    private static String ExtractFile(DirectoryInfo output, (string Name, Assembly Assembly) resourceGroup, string resourceName,
        ICopyrightManager copyrightManager)
    {
        var path = Path.Combine(GetFilePath(resourceGroup.Name, resourceName));
        var filename = Path.Combine(output.FullName, path);
        Directory.CreateDirectory(Path.GetDirectoryName(filename)!);
        using var outputFile = File.Create(filename);
        copyrightManager.AddCopyright(outputFile, filename);
        resourceGroup.Assembly.GetManifestResourceStream(resourceName)!.CopyTo(outputFile);
        return path;
    }

    public static string[] GetFilePath(string root, string resourceName)
    {
        var filepath = resourceName.Replace(root + ".", string.Empty);
        var parts = filepath.Split('.');
        if (parts.Last() == "qmldir")
            return parts;

        var filename = parts[^2] + "." + parts[^1];
        return parts.Take(parts.Length - 2).Append(filename).ToArray();
    }
}