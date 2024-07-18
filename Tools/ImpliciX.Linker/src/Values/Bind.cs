namespace ImpliciX.Linker.Values;

public class Bind
{
    public Bind(string definition)
    {
        var items = definition.Split(':', StringSplitOptions.TrimEntries);
        SourcePath = new FileInfo(items[0]);
        DestinationPath = items[1];
    }

    public FileInfo SourcePath { get; }
    public string DestinationPath { get; }

    public static bool IsInvalid(string definition)
    {
        try
        {
            var b = new Bind(definition);

            return b.SourcePath.Attributes.HasFlag(FileAttributes.Directory) ? !Directory.Exists(b.SourcePath.FullName) : !File.Exists(b.SourcePath.FullName);
        }
        catch
        {
            return true;
        }
    }
}