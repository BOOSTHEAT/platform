using System.Diagnostics;

namespace ImpliciX.Linker;

public static class Tools
{
    public class ExecutionResult
    {
        public int ExitCode { get; }
        public string StandardOutput { get; }
        public string StandardError { get; }

        public ExecutionResult(Process p)
        {
            ExitCode = p.ExitCode;
            StandardOutput = p.StartInfo.RedirectStandardOutput ? p.StandardOutput.ReadToEnd() : "";
            StandardError = p.StartInfo.RedirectStandardError ? p.StandardError.ReadToEnd() : "";
        }
    }

    public static ExecutionResult Build(FileInfo application)
    {
        using var process = new Process();
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"build --configuration Release \"{application.FullName}\"";
        Console.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.Start();
        process.WaitForExit();
        return new ExecutionResult(process);
    }

    public static ExecutionResult Restore(FileInfo application, bool diagnostics,string target = "")
    {
        using var process = new Process();
        var verbosity = diagnostics ? "detailed" : "minimal";
        process.StartInfo.FileName = "dotnet";
        var sourceFolder = Directory.GetParent(application.FullName)?.FullName;
        target = target == "" ? "" : $"--runtime {target}";
        process.StartInfo.Arguments = $"restore \"{application.FullName}\" --verbosity {verbosity} --configfile \"{sourceFolder}/NuGet.Config\" {target}";
        Console.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.Start();
        process.WaitForExit();
        return new ExecutionResult(process);
    }

    public static ExecutionResult Run(FileInfo application, bool diagnostics)
    {
        using var process = new Process();
        var verbosity = diagnostics ? "detailed" : "minimal";
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"run --no-restore --configuration Release --project \"{application.FullName}\" --verbosity {verbosity}";
        Console.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.Start();
        process.WaitForExit();
        return new ExecutionResult(process);
    }

    public static ExecutionResult Publish(string projectPath, string outputFolder, bool diagnostics, string target)
    {
        using var process = new Process();

        var options = $"--configuration Release --framework {DotnetCommand.SdkVersion} --runtime {target} --self-contained false --no-restore";
        var verbosity = diagnostics ? "detailed" : "minimal";
        process.StartInfo.FileName = "dotnet";
        process.StartInfo.Arguments = $"publish {options} --verbosity {verbosity} --output \"{outputFolder}\" \"{projectPath}\"";
        Console.WriteLine($"{process.StartInfo.FileName} {process.StartInfo.Arguments}");
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = false;
        process.StartInfo.RedirectStandardError = false;
        process.Start();
        process.WaitForExit();
        Directory.EnumerateFiles(outputFolder, "*.pdb").ToList().ForEach(File.Delete);
        return new ExecutionResult(process);
    }

    public static DirectoryInfo GetTempFolder(params string[] names)
    {
        var all = names.Prepend("ImpliciX.Linker").Prepend(Path.GetTempPath()).ToArray();
        var path = Path.Combine(all);
        return Directory.CreateDirectory(path);
    }
}