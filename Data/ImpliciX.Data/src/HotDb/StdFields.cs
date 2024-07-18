using System;
using System.IO;
using System.Text;
using ImpliciX.Data.HotDb.Model;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.HotDb;

public static class StdFields
{
  public enum StorageType : byte
  {
    Float = 1,
    Text = 2,
    Long = 3,
  }

  public const int BytesPerChar = 4;
  private const char PaddingChar = '\0';

  public static FieldDef Create<T>(string name) => Create(name, typeof(T));

  public static FieldDef Create(string name, Type type)
  {
    var (storageType, size) = GetSizeInBytes(type);
    return new FieldDef(name, type.FullName!, (byte)storageType, size);
  }

  private static (StorageType storageType, int size) GetSizeInBytes(Type type)
  {
    if (type == typeof(float))
      return (StorageType.Float, sizeof(float));
    if (type == typeof(long))
      return (StorageType.Long, sizeof(long));
    if (type.IsEnum)
      return (StorageType.Float, sizeof(float));
    if (typeof(Text10).IsAssignableFrom(type))
      return (StorageType.Text, 10 * BytesPerChar);
    if (typeof(Text50).IsAssignableFrom(type))
      return (StorageType.Text, 50 * BytesPerChar);
    if (typeof(Literal).IsAssignableFrom(type) || (typeof(Text200).IsAssignableFrom(type)))
      return (StorageType.Text, 200 * BytesPerChar);
    if (typeof(IFloat).IsAssignableFrom(type))
      return (StorageType.Float, sizeof(float));
    throw new NotSupportedException($"Type : {type.Name} not supported");
  }

  public static object ReadFrom(this FieldDef field, BinaryReader br)
  {
    var fieldBytes = br.ReadBytes(field.FixedSizeInBytes);
    var fieldRawValue = field.FromBytes(fieldBytes);
    return fieldRawValue;
  }

  public static object FromBytes(this FieldDef field, byte[] fieldBytes)
  {
    var fieldRawValue = (object)(field.StorageType switch
    {
      (byte)StorageType.Float => BitConverter.ToSingle(fieldBytes),
      (byte)StorageType.Text => Encoding.UTF32.GetString(fieldBytes).TrimEnd(PaddingChar),
      (byte)StorageType.Long => BitConverter.ToInt64(fieldBytes),
      _ => throw new NotSupportedException()
    });
    return fieldRawValue;
  }

  public static void WriteTo(this FieldDef field, BinaryWriter bw, object value)
  {
    var fieldBytes = field.ToBytes(value);
    bw.Write(fieldBytes);
  }

  public static byte[] ToBytes(this FieldDef field, object value)
  {
    switch (value)
    {
      case null when field.StorageType == (byte)StorageType.Text:
        return Encoding.UTF32.GetBytes(FixedLengthString(string.Empty, field.FixedSizeInBytes / BytesPerChar));
      case null when field.StorageType == (byte)StorageType.Float:
        return BitConverter.GetBytes(float.NaN);
      case float f:
        return BitConverter.GetBytes(f);
      case long l:
        return BitConverter.GetBytes(l);
      case Enum e:
        return BitConverter.GetBytes(Convert.ToSingle(e));
      case IFloat f:
        return BitConverter.GetBytes(f.ToFloat());
      case ITextValue t:
        return Encoding.UTF32.GetBytes(FixedLengthString(t.ToString(), field.FixedSizeInBytes / BytesPerChar));
      default:
        throw new NotSupportedException();
    }

    string FixedLengthString(string s, int length)
    {
      if (s.Length > length) throw new InvalidOperationException($"String {s} is longer than {length}");
      return s.PadRight(length, PaddingChar);
    }
  }
}