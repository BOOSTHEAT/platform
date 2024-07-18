using System;
using System.IO;
using ImpliciX.Data;

namespace ImpliciX.Linker.FileSystemOperations;

public class SymbolicLinkOperation : FileSystemOperation
{
  public FileInfo Source { get; }
  public string Target { get; }

  public SymbolicLinkOperation(FileInfo source, string target)
  {
    Source = source;
    Target = target;
  }

  public override string ToString()
  {
    return $"Link {Target} to {Source.FullName}";
  }

  protected override string Execute(Func<string, string> virtualDestination)
  {
    var destination = virtualDestination(Target);
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    var sl = Fs.TryCreateSymbolicLink(Source.FullName,destination);
    return $"Created {sl.FullName}";
  }
}