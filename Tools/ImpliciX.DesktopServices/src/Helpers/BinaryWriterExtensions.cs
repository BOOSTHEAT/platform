using System;
using System.IO;
using System.Text;
using Renci.SshNet.Common;

namespace ImpliciX.DesktopServices.Helpers;

internal static class BinaryWriterExtensions
{
  public static void EncodeNullTerminatedString(this BinaryWriter writer, string str)
  {
    writer.Write(Encoding.ASCII.GetBytes(str));
    writer.Write('\0');
  }

  public static void EncodeBinary(this BinaryWriter writer, string str)
  {
    EncodeBinary(writer, Encoding.ASCII.GetBytes(str));
  }

  public static void EncodeBinary(this BinaryWriter writer, MemoryStream str)
  {
    EncodeBinary(writer, str.ToArray());
  }

  public static void EncodeBinary(this BinaryWriter writer, byte[] str)
  {
    EncodeUInt(writer, (uint)str.Length);
    writer.Write(str);
  }

  public static void EncodeBinary(this BinaryWriter writer, BigInteger bigInteger)
  {
    var data = bigInteger.ToByteArray().Reverse();
    EncodeUInt(writer, (uint)data.Length);
    writer.Write(data);
  }

  public static void EncodeUInt(this BinaryWriter writer, uint i)
  {
    var data = BitConverter.GetBytes(i);
    if (BitConverter.IsLittleEndian)
      Array.Reverse(data);
    writer.Write(data);
  }

  public static void EncodeInt(this BinaryWriter writer, int i)
  {
    var data = BitConverter.GetBytes(i);
    if (BitConverter.IsLittleEndian)
      Array.Reverse(data);
    writer.Write(data);
  }
}