using System.CommandLine;
using ImpliciX.Data;
using ImpliciX.Linker.Values;

namespace ImpliciX.Linker;

public class Pack : GenericCommand
{
    public static Command CreateCommand()
    {
        var command = new Command("pack",
            "Create an Harmony package")
        {
            new Option<string>(
                new[] { "-n", "--name" },
                "Package name").Required(),
            new Option<string>(
                new[] { "-v", "--version" },
                "Package version").VersionNumberOnly().Required(),
            new Option<PartReference>(
                    new[] { "-p", "--part" },
                    "Part reference <id>,<version>,<path>")
                { Arity = ArgumentArity.OneOrMore }.InvalidWhen(PartReference.IsInvalid).Required(),
            new Option<string>(
                new[] { "-o", "--output" },
                "Output zip file").LegalFilePathsOnly().NonExistingOnly().Required(),
            new Option<Bind>(
                new[] { "-b", "--bind" },
                "Folder to bind <source_path>,<destination_path>") { Arity = ArgumentArity.ZeroOrMore }.InvalidWhen(Bind.IsInvalid),
        };
        var packer = new Pack(command);
        return command;
    }

    private Pack(Command command) : base(command)
    {
    }

    protected override int Execute(Dictionary<string, object> arguments)
    {
        try
        {
            var parts = ((List<PartReference>)arguments["part"]).Select(ToPartData).ToArray();
            var directoriesBind = arguments.TryGetValue("bind", out var argument) ? ((List<Bind>)argument).Select(x => (x.SourcePath.FullName, x.DestinationPath)).ToList() : new List<(string, string)>();
            
            var output = (string)arguments["output"];
            var manifest = new Manifest
            {
                Device = (string)arguments["name"],
                Revision = (string)arguments["version"],
                Date = DateTime.Now,
                Content = new Manifest.ContentData
                {
                    APPS = parts.Where(x => x.category == "APPS").Select(x => x.part).ToArray(),
                    MCU = parts.Where(x => x.category == "MCU").Select(x => x.part).ToArray(),
                    BSP = parts.Where(x => x.category == "BSP").Select(x => x.part).ToArray(),
                }
            };
            Console.WriteLine($"Creating package {manifest.Device} {manifest.Revision} into {output}");
            foreach (var (category, partref, part) in parts)
                Console.WriteLine($"  Id:{part.Target}\tVersion:{part.Revision}\tFile:{partref.Path.FullName}");
            Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            foreach (var bind in directoriesBind)
                Console.WriteLine($"  Bind:{bind.Item1}\tTo:{bind.Item2}");
            var result = PackageWriter.Write(manifest, parts.Select(x => x.partref.Path.FullName), output, directoriesBind);
            if (result.IsError)
                throw new Exception(result.Error.Message);
            Console.WriteLine("Packing complete.");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine("Packing failed.");
            Console.WriteLine(e.Message);
            return e.HResult;
        }
    }

    private (string category, PartReference partref, Manifest.PartData part) ToPartData(PartReference part) =>
        (part.Category, part, new Manifest.PartData
        {
            Target = part.Id,
            Revision = part.Version,
            FileName = part.Path.Name
        });
}