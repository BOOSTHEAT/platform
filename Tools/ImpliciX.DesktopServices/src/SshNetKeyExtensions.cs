using System;
using System.IO;
using ImpliciX.DesktopServices.Helpers;
using Renci.SshNet;
using Renci.SshNet.Security;

namespace ImpliciX.DesktopServices;

public static class SshNetKeyExtensions
{
  public static string ToPublic(this PrivateKeyFile keyFile)
  {
    return ((KeyHostAlgorithm) keyFile.HostKey).Key.ToPublic();
  }
      
  public static string ToPublic(this Key key)
  {
    using var pubStream = new MemoryStream();
    using var pubWriter = new BinaryWriter(pubStream);
    key.PublicKeyData(pubWriter);
    var base64 = Convert.ToBase64String(pubStream.ToArray());
    return $"{key} {base64}";
  }
}