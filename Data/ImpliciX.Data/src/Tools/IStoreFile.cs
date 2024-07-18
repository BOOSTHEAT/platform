using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ImpliciX.Data.Tools;

public interface IStoreFile : IStoreItem
{
  Task<Stream> OpenReadAsync();
  Task<Stream> OpenWriteAsync();

  static Task<string> ReadAllTextAsync(IStoreFile file)
  {
    var stream = file.OpenReadAsync().Result;
    var streamReader = new StreamReader(stream, Encoding.UTF8);
    return streamReader.ReadToEndAsync();
  }

  static void WriteAllText(IStoreFile storeFile, string text)
  {
    using (var stream = OpenStream(storeFile))
    {
      WriteLine(stream, text);
    }
  }

  static void WriteAllLinesAsync(IStoreFile storeFile, IEnumerable<string> lines)
  {
    using (var stream = OpenStream(storeFile))
    {
      foreach (var line in lines)
      {
        WriteLine(stream, line + Environment.NewLine);
      }
    }
  }

  private static Stream OpenStream(IStoreFile storeFile) => storeFile
    .OpenWriteAsync()
    .Result;

  private static void WriteLine(Stream stream, string line)
  {
    stream
      .WriteAsync(
        Encoding.Default.GetBytes(
          line
        )
      )
      ;
  }

  static void Copy(IStoreFile storeFile, IStoreFile destinationFile)
  {
    var src = storeFile.OpenReadAsync().Result;
    var dest = destinationFile.OpenWriteAsync().Result;
    src.CopyTo(dest);
    src.Close();
    dest.Flush();
    dest.Close();
  }

  static IStoreFile CreateFile(IStoreFolder destination, string filename) =>
    destination.CreateFileAsync(filename).Result;
}
