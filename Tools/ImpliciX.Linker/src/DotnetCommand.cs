using System.CommandLine;
using ImpliciX.Linker.Values;

namespace ImpliciX.Linker;

public abstract class DotnetCommand : GenericCommand
{
  protected DotnetCommand(Command command) : base(command)
  {
  }

  protected static void AddOptionsTo(Command command)
  {
    new Option[] {
    new Option<FeedReference>(
      new[] { "-n", "--nupkg" },
      "Reference to nupkg <name>[:<version>]") { Arity = ArgumentArity.ZeroOrMore }.InvalidWhen(FeedReference.IsInvalid),
    new Option<FileInfo>(
      new[] { "-a", "--assembly" },
      "Full path to assembly") { Arity = ArgumentArity.ZeroOrMore }.ExistingOnly(),
    new Option<FileInfo>(
      new[] { "-p", "--project" },
      "Full path to dotnet project") { Arity = ArgumentArity.ZeroOrMore }.ExistingOnly(),
    new Option<string>(
      new[] { "-e", "--entry" },
      "Application entry point (name of a class deriving from RuntimeModel)").CodeIdentifier().Required(),
    new Option<string>(
      new[] { "-s", "--source" },
      "Nuget source file") { Arity = ArgumentArity.ZeroOrMore }.LegalFilePathsOnly(),
    new Option<bool>(
      new[] { "--diagnostics" }, () => false,
      "Activate for detailed logs"),
    }.ToList().ForEach(command.AddOption);
  }

  protected override int Execute(Dictionary<string, object> arguments)
  {
    var entry = (string) arguments["entry"];
    var appName = entry.Replace(".", "");
    return Execute(GetReferences(arguments), appName, entry, arguments);
  }

  protected abstract int Execute(IEnumerable<string> references, string appName, string entry,
    Dictionary<string, object> arguments);


  private static IEnumerable<string> GetReferences(Dictionary<string, object> arguments)
  {
    var nupkgs = GetOptions<FeedReference>(arguments, "nupkg")
      .Select(feedReference =>
        $"<PackageReference Include=\"{feedReference.Name}\" Version=\"{feedReference.Version}\" />");
    var assemblies = GetOptions<FileInfo>(arguments, "assembly")
      .Select(fileInfo => $"<Reference Include=\"{fileInfo.FullName}\" />");
    var projects = GetOptions<FileInfo>(arguments, "project")
      .Select(fileInfo => $"<ProjectReference Include=\"{fileInfo.FullName}\" />");
    var references = nupkgs.Concat(assemblies).Concat(projects);
    return references;
  }

  private static IEnumerable<T> GetOptions<T>(Dictionary<string, object> arguments, string optionName) =>
    arguments.TryGetValue(optionName, out var option)
      ? (IEnumerable<T>)option
      : Enumerable.Empty<T>();

  public const string SdkVersion = "net8.0";
}