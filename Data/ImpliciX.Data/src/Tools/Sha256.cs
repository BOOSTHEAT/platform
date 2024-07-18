using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using ImpliciX.Data.Tools;

namespace ImpliciX.Data;

public static class Sha256
{
  public static string OfFile(string filePath)
  {
    using var sha256 = SHA256.Create();
    var hash = sha256.ComputeHash(File.ReadAllBytes(filePath));
    return string.Concat(hash.Select(b => b.ToString("x2")));
  }

  public static string OfFile(IStoreFile storeFile)
  {
    using var sha256 = SHA256.Create();
    var stream = storeFile.OpenReadAsync().Result;
    var streamEnd = Convert.ToInt32(stream.Length);

    var bytes = new byte[streamEnd];
    var read = stream.Read(bytes, 0, streamEnd);
    var hash = sha256.ComputeHash(bytes);
    return string.Concat(hash.Select(b => b.ToString("x2")));
  }
}
