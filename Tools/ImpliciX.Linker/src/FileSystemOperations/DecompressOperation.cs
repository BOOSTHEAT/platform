using System;
using System.IO;
using ImpliciX.Data;

namespace ImpliciX.Linker.FileSystemOperations;

public class DecompressOperation : FileSystemOperation
{
  public FileInfo Source { get; }
  public FileInfo Destination { get; }

  public DecompressOperation(FileInfo source, FileInfo destination)
  {
    Source = source;
    Destination = destination;
  }

  public override string ToString()
  {
    return $"Decompress {Source.FullName} to {Destination.FullName}";
  }
  
  protected override string Execute(Func<string, string> virtualDestination)
  {
    var destination = virtualDestination(Destination.FullName);
    Zip.ExtractToDirectory(Source.FullName, destination);
    return $"Created {destination}";
  }
}