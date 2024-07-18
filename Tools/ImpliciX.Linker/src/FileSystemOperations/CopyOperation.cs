using System;
using System.IO;

namespace ImpliciX.Linker.FileSystemOperations;

public class CopyOperation : FileSystemOperation
{
  public FileInfo Source { get; }
  public FileInfo Destination { get; }

  public CopyOperation(FileInfo source, FileInfo destination)
  {
    Source = source;
    Destination = destination;
  }

  public override string ToString()
  {
    return $"Copy {Source.FullName} to {Destination.FullName}";
  }
  
  protected override string Execute(Func<string, string> virtualDestination)
  {
    var destination = virtualDestination(Destination.FullName);
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    Source.CopyTo(destination);
    return $"Created {destination}";
  }
}