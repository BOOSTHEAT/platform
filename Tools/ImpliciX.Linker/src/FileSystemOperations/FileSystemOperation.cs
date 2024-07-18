using System;
using System.IO;

namespace ImpliciX.Linker.FileSystemOperations;

public abstract class FileSystemOperation
{
  public virtual string Execute(string virtualDestination) =>
    Execute(path => Path.Combine(virtualDestination, path.Substring(1)));

  protected abstract string Execute(Func<string,string> virtualDestination);
}