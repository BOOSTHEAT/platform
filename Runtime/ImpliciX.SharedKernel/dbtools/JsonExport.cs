using System.Text.Json;
using ImpliciX.Data.HotDb;
using ImpliciX.Data.HotTimeSeries;

namespace ImpliciX.SharedKernel.HotDbTools;
public class JsonExport : GenericCommand
{
    public static Command CreateCommand()
    {
        var command = new Command("export",
            "Export a hot database to a json file")
        {
            new Option<string>(
                new[] {"--db"},
                "The database folder path").Required().ExistingFolderOnly()
        };
        _ = new JsonExport(command);
        return command;
    }

    private JsonExport(Command command) : base(command)
    {
    }

    protected override int Execute(Dictionary<string, object> arguments)
    {
        Console.WriteLine("Execute json export");
        var dbPath = (string) arguments["db"];

        var outputPath = Path.Combine(
            Path.GetTempPath(), 
            Directory.GetParent(dbPath)!.Name);
        
        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);
        
        var outputFile = Path.Combine(outputPath, "export.json");

        HotDbExport.ExportJson(
            dbPath:dbPath,
            exportPath:outputFile, 
            decodeFunc:(b,s)=>TimeSeriesDbExt.FromBytes(b,s));

        Console.WriteLine($"Exported to {outputFile}");
        return 0;
    }
    
 
}