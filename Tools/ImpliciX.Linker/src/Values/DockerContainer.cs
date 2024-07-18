namespace ImpliciX.Linker.Values;

public class DockerContainer
{
    public DockerContainer(string definition)
    {
        var items = definition.Split(',', StringSplitOptions.TrimEntries);
        if (items.Length != 3)
            throw new Exception("Unexpected docker image definition");
        Target = items[0];
        ContainerName = items[1];
        RelativeAppPath = items[2];
    }

    public string Target { get; }
    public string ContainerName { get; }
    public string RelativeAppPath { get; }

    public static bool IsInvalid(string definition)
    {
        try
        {
            var items = definition.Split(',', StringSplitOptions.TrimEntries);
            return (items.Length != 3);
        }
        catch
        {
            return true;
        }
    }
}