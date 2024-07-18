using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ImpliciX.Data;
using ImpliciX.Language.Model;
using ImpliciX.Linker.FileSystemOperations;
using ImpliciX.Linker.Values;

namespace ImpliciX.Linker;

public class DataFs : GenericCommand
{
  public static Command CreateCommand()
  {
    var command = new Command("datafs",
      "Create a datafs and, optionally, add it to Yocto/Toradex image")
    {
      new Option<Uri>(
        new[] { "-p", "--package" },
        "Release package to install in the datafs").Required(),
      new Option<string>(
          new[] { "-d", "--destination" },
          "Destination folders for linking executables")
        { Arity = ArgumentArity.ZeroOrMore }.LegalFilePathsOnly().Required(),
      new Option<ExeInstall>(
          new[] { "-e", "--exe" },
          "Executable install <urn>,<path>")
        { Arity = ArgumentArity.ZeroOrMore }.InvalidWhen(ExeInstall.IsInvalid),
      new Option<SourceAndTarget>(
          new[] { "-f", "--file" },
          "File install <source>,<target>")
        { Arity = ArgumentArity.ZeroOrMore }.InvalidWhen(SourceAndTarget.IsInvalid),
      new Option<SourceAndTarget>(
          new[] { "-l", "--link" },
          "Create link <source>,<target>")
        { Arity = ArgumentArity.ZeroOrMore }.InvalidWhen(SourceAndTarget.IsInvalid),
      new Option<string>(
        new[] { "-o", "--output" },
        "Output gz file").LegalFilePathsOnly().NonExistingOnly().Required(),
      new Option<FileInfo>(
        new[] { "-i", "--image" },
        "Yocto/Toradex image definition (json) to update").ExistingOnly()
    };
    var cmd = new DataFs(command);
    return command;
  }

  public DataFs(Command command) : base(command)
  {
  }

  protected override int Execute(Dictionary<string, object> args)
  {
    try
    {
      var package = ReadPackage(args);
      var manifestFile = Path.GetTempFileName();
      package!.CopyManifest(manifestFile);
      args["manifest-file"] = manifestFile;
      var exeOperations = FindOperations(args, package);
      var virtualDestination = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      Console.WriteLine($"Generating file system in {virtualDestination}");
      Directory.CreateDirectory(virtualDestination);
      foreach (var operation in exeOperations)
      {
        Console.WriteLine($"    {operation}");
        Console.WriteLine($"        {operation.Execute(virtualDestination)}");
      }

      CreateTarball(args, virtualDestination);
      UpdateImage(args, package);
      return 0;
    }
    catch (Exception e)
    {
      Console.WriteLine("Datafs creation failed.");
      Console.WriteLine(e.Message);
      return e.HResult;
    }
  }

  public static Package? ReadPackage(Dictionary<string, object> args)
  {
    if (!args.TryGetValue("package", out object? uri))
      return null;
    var packageUri = (Uri)uri;
    var result = PackageLoader.Load(new PackageLocation(packageUri), x => new SoftwareDeviceNode(x, null));
    if (result.IsError)
      throw new Exception($"Failed to load package {packageUri}: {result.Error.Message}");
    return result.Value;
  }

  public static IEnumerable<FileSystemOperation> FindOperations(Dictionary<string, object> args, Package? package) =>
    new Func<Dictionary<string, object>, Package?, IEnumerable<FileSystemOperation>>[]
    {
      FindExeOperations, FindFileOperations, FindLinkOperations
    }.SelectMany(f => f(args, package));

  private static IEnumerable<FileSystemOperation> FindExeOperations(Dictionary<string, object> args, Package? package)
  {
    if (package == null)
      return Enumerable.Empty<FileSystemOperation>();
    var executables =
      from content in package._contents.Values
      from exeInstall in ((List<ExeInstall>)args["exe"]).ToArray()
      where content.DeviceNode.Urn.Value == exeInstall.Urn
      let name = Path.GetFileName(exeInstall.Path)
      let decompressInto = new FileInfo(Path.Combine(exeInstall.Path, content.Revision))
      let copyInto = new FileInfo(Path.Combine(decompressInto.FullName, content.ContentFile.Name))
      let operation = CopyOrDecompress(content.ContentFile, copyInto, decompressInto)
      select (name, decompressInto, operation);
    var exeDestinationFolders = ((List<string>)args["destination"]).ToArray();
    var manifestOperations = exeDestinationFolders.Select(folder =>
      new CopyOperation(new FileInfo((string)args["manifest-file"]),
        new FileInfo(Path.Combine(folder, "manifest.json")))
    );
    var exeOperations = executables.SelectMany(x =>
      exeDestinationFolders.Select(folder =>
        new SymbolicLinkOperation(x.decompressInto, Path.Combine(folder, x.name))
      ).Prepend(x.operation)
    );
    return manifestOperations.Concat(exeOperations);
  }

  private static IEnumerable<FileSystemOperation>
    FindFileOperations(Dictionary<string, object> args, Package? package) =>
    args.TryGetValue("file", out object? files)
      ? ((List<SourceAndTarget>)files).ToArray().Select(file => CopyOrDecompress(file.Source, file.Target, file.Target))
      : Enumerable.Empty<FileSystemOperation>();

  private static IEnumerable<FileSystemOperation>
    FindLinkOperations(Dictionary<string, object> args, Package? package) =>
    args.TryGetValue("link", out object? files)
      ? ((List<SourceAndTarget>)files).ToArray()
      .Select(file => new SymbolicLinkOperation(file.Source, file.Target.FullName))
      : Enumerable.Empty<FileSystemOperation>();

  private static FileSystemOperation CopyOrDecompress(FileInfo source, FileInfo copyDestination,
    FileInfo decompressDestination) =>
    source.Extension == ".zip"
      ? new DecompressOperation(source, decompressDestination)
      : new CopyOperation(source, copyDestination);

  private static void CreateTarball(Dictionary<string, object> args, string virtualDestination)
  {
    var output = (string)args["output"];
    Directory.CreateDirectory(Path.GetDirectoryName(output)!);
    using var gz = Compression.CreateGz(output);
    gz.MakeTar(virtualDestination);
  }

  private static void UpdateImage(Dictionary<string, object> args, Package? package)
  {
    if (!args.TryGetValue("image", out object? img) || package == null)
      return;
    var image = (FileInfo)img;
    var newContent =
      UpdateImageContent(File.ReadAllText(image.FullName), new FileInfo((string)args["output"]), package);
    File.WriteAllText(image.FullName, newContent);
  }

  public static string UpdateImageContent(string imageContent, FileInfo tarball, Package package)
  {
    var dataPartition = new JsonObject
    {
      ["partition_size_nominal"] = 512,
      ["want_maximised"] = true,
      ["content"] = new JsonObject
      {
        ["label"] = "DATA",
        ["filesystem_type"] = "ext4",
        ["mkfs_options"] = "-E nodiscard",
        ["filename"] = tarball.Name,
        ["uncompressed_size"] = tarball.Length / 1000000
      }
    };
    var json = JsonNode.Parse(imageContent);
    var partitions = json!["blockdevs"]![0]!["partitions"]!.AsArray();
    partitions.RemoveAt(partitions.Count - 1);
    partitions.Add(dataPartition);
    json["name"] = $"{package.ApplicationName} MMI Image";
    json["version"] = package.Revision;
    json["release_date"] = package.Date.ToString("yyyy-MM-dd");
    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
    return json?.ToJsonString(options)!;
  }
}