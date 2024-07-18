using System.CommandLine;
using System.IO.Compression;
using ImpliciX.Data;
using Newtonsoft.Json.Linq;
using ImpliciX.Linker.Values;

namespace ImpliciX.Linker;

public class DockerPack : GenericCommand
{
    private const string ManifestFile = "manifest.json";

    public static Command CreateCommand()
    {
        var containerCommand = new Command("dockerpack", "Add a container");
        AddOptions(containerCommand);
        var command = new DockerPack(containerCommand);
        return containerCommand;
    }
    
    public DockerPack(Command command) : base(command)
    {
    }

    private static void AddOptions(Command containerCommand)
    {
        containerCommand.AddOption(new Option<string>(new []{"-p","--package"}, "Specify the implicix package")
            { Arity = ArgumentArity.ExactlyOne }.Required().LegalFilePathsOnly());
        containerCommand.AddOption(new Option<DockerContainer>(new []{"-r","--run"}, "Specify the app <target>,<container_name>,<relative_app_path>")
                { Arity = ArgumentArity.OneOrMore }.Required()
            .InvalidWhen(DockerContainer.IsInvalid));
        containerCommand.AddOption(new Option<string>(new []{"-f","--file"}, "Specify additional file or folder")
            { Arity = ArgumentArity.ZeroOrMore }.LegalFilePathsOnly());
        containerCommand.AddOption(new Option<string>(new []{"-c","--compose"}, "Specify the docker compose file")
                { Arity = ArgumentArity.ExactlyOne }.Required()
            .LegalFilePathsOnly());
        containerCommand.AddOption(new Option<string>(new []{"-o","--output"}, "Specify the output file")
            { Arity = ArgumentArity.ExactlyOne }.Required().LegalFilePathsOnly());
    }
    
    internal static ExecutionContext ParseArguments(Dictionary<string, object> args)
    {
        return new ExecutionContext
        {
            Package = args["package"] as string,
            ComposeFile = args["compose"] as string,
            OutputFile = args["output"] as string,
            Containers = args["run"] as List<DockerContainer>,
            Files = args["file"] as List<string>
        };
    }

    protected override int Execute(Dictionary<string, object> args)
    {
        try
        {
            var executionContext = ParseArguments(args);
            PreparePackage(executionContext);
            CreateDockerPackage(executionContext);
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("Docker Pack creation failed.");
            Console.WriteLine(e.Message);
            return e.HResult;
        }
    }

    private void PreparePackage(ExecutionContext context)
    {
        context.TmpDirPath = CreateRandomTempDirectory();
        context.TmpSourcePath = Path.Combine(context.TmpDirPath, "source");
        context.DockerPackagePath = Path.Combine(context.TmpDirPath, "package");
        context.ManifestPath = Path.Combine(context.TmpSourcePath, ManifestFile);

        UnzipRecursively(context.Package, context.TmpSourcePath);
    }

    private void CreateDockerPackage(ExecutionContext context)
    {
        ProcessImages(context);
        ProcessAdditionalFiles(context);
        FinalizePackage(context);
    }

    private void ProcessImages(ExecutionContext context)
    {
        foreach (var image in context.Containers)
        {
            var manifest = File.ReadAllText(context.ManifestPath);
            var (fileName, revision) = GetFileNameAndRevisionForTarget(manifest, image.Target);
            var appSourcePath = Path.Combine(context.TmpSourcePath, revision, fileName.Replace(".zip", ""));
            var appDestinationPath = Path.Combine(context.DockerPackagePath, image.RelativeAppPath);
            CopyFileOrDirectory(appSourcePath, appDestinationPath);
        }
    }

    private void ProcessAdditionalFiles(ExecutionContext context)
    {
        foreach (var file in context.Files)
        {
            CopyFileOrDirectory(file, Path.Combine(context.DockerPackagePath, Path.GetFileName(file)));
        }
    }

    private void FinalizePackage(ExecutionContext context)
    {
        CopyFileOrDirectory(context.ManifestPath, Path.Combine(context.DockerPackagePath, Path.GetFileName(context.ManifestPath)));
        CopyFileOrDirectory(context.ComposeFile, Path.Combine(context.DockerPackagePath, Path.GetFileName(context.ComposeFile)));
        if (File.Exists(context.OutputFile)) File.Delete(context.OutputFile);
        ZipFile.CreateFromDirectory(context.DockerPackagePath, context.OutputFile);
    }

    private static void CopyFileOrDirectory(string source, string destination)
    {
        if (File.Exists(source))
        {
            File.Copy(source, destination, true);
        }
        else if (Directory.Exists(source))
        {
            Directory.CreateDirectory(destination);
            foreach (var file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                CopyFileOrDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
            }
        }
    }

    private static void UnzipRecursively(string zipPath, string extractPath)
    {
        Zip.ExtractToDirectory(zipPath, extractPath);
        foreach (var file in Directory.GetFiles(extractPath, "*.zip", SearchOption.AllDirectories))
        {
            UnzipRecursively(file, Path.Combine(extractPath, Path.GetFileNameWithoutExtension(file)));
            File.Delete(file);
        }
    }

    private static string CreateRandomTempDirectory()
    {
        var fullPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(fullPath);
        return fullPath;
    }

    internal static (string, string) GetFileNameAndRevisionForTarget(string jsonManifest, string target)
    {
        var jsonObject = JObject.Parse(jsonManifest);
        var apps = jsonObject["Content"]?["APPS"] as JArray;
        return (from app in apps where app["Target"].ToString() == target select (app["FileName"].ToString(), app["Revision"].ToString())).FirstOrDefault();
    }
}

public class ExecutionContext
{
    public string Package { get; set; }
    public string ComposeFile { get; set; }
    public string OutputFile { get; set; }
    public List<DockerContainer> Containers { get; set; }
    public List<string> Files { get; set; }
    public string TmpDirPath { get; set; }
    public string TmpSourcePath { get; set; }
    public string DockerPackagePath { get; set; }
    public string ManifestPath { get; set; }
}