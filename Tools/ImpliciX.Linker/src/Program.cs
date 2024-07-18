using System.CommandLine;
using System.Reflection;

namespace ImpliciX.Linker;

internal static class Program
{
    public static int Main(string[] args)
    {
        var command = new RootCommand("Build and package an application for the Connect platform.")
        {
            Build.CreateCommand(),
            Qml.CreateCommand(),
            Pack.CreateCommand(),
            DataFs.CreateCommand(),
            DockerPack.CreateCommand()
        };
        var linker = Assembly.GetExecutingAssembly().GetName();
        Console.WriteLine($"{linker.Name} {linker.Version}");
        return command.Invoke(args);
    }
}