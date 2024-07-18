using System;
using System.IO;
using ImpliciX.Data.HotDb;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.HotDb;

public class StdFieldsTests
{
  private static TestCaseData[] _fieldCases = new[]
  {
    new TestCaseData(123L, 123L, 8),
    new TestCaseData(123.456f, 123.456f, 4),
    new TestCaseData(Presence.Enabled, 1f, 4),
    new TestCaseData(Temperature.Create(273.15f), 273.15f, 4),
    new TestCaseData(Text10.Create("foo"), "foo", 40),
    new TestCaseData(Text50.Create("foo"), "foo", 200),
    new TestCaseData(Text200.Create("foo"), "foo", 800),
    new TestCaseData(Literal.Create("foo"), "foo", 800),
  };
  
  [TestCaseSource(nameof(_fieldCases))]
  public void CanSerializeToBytesThenDeserialize(object obj, object expected, long expectedSize)
  {
    CanSerializeToBytesThenDeserialize(obj.GetType(), obj, expected, expectedSize);
  }

  private static TestCaseData[] _fieldTypes = new[]
  {
    new TestCaseData(typeof(float), float.NaN, 4),
    new TestCaseData(typeof(IFloat), float.NaN, 4),
    new TestCaseData(typeof(Text10), string.Empty, 40),
    new TestCaseData(typeof(Text50), string.Empty, 200),
    new TestCaseData(typeof(Text200), string.Empty, 800),
    new TestCaseData(typeof(Literal), string.Empty, 800),
  };
  
  [TestCaseSource(nameof(_fieldTypes))]
  public void CanSerializeNullToBytesThenDeserialize(Type type, object expected, long expectedSize)
  {
    CanSerializeToBytesThenDeserialize(type, null, expected, expectedSize);
  }
  
  private void CanSerializeToBytesThenDeserialize(Type type, object obj, object expected, long expectedSize)
  {
    var field = StdFields.Create("whatever", type);
    using var ms = new MemoryStream();
    using var bw = new BinaryWriter(ms);
    field.WriteTo(bw, obj);
    Assert.That(ms.Length, Is.EqualTo(expectedSize));
    ms.Seek(0, SeekOrigin.Begin);
    using var br = new BinaryReader(ms);
    var actual = field.ReadFrom(br);
    Assert.That(actual, Is.EqualTo(expected));
  }
  
  [TestCase(typeof(string))]
  public void CannotCreateField(Type type)
  {
    Assert.Throws<NotSupportedException>(() =>
    {
      var field = StdFields.Create("whatever", type);
    });
  }
  
  [TestCase(typeof(long))]
  public void CannotSerializeNullToBytes(Type type)
  {
    var field = StdFields.Create("whatever", type);
    Assert.Throws<NotSupportedException>(() =>
    {
      var result = field.ToBytes(null);
    });
  }

  [Test]
  public void CannotSerializeTooLongStringToBytes()
  {
    var field = StdFields.Create("whatever", typeof(Text10));
    Assert.Throws<InvalidOperationException>(() =>
    {
      var result = field.ToBytes(Text50.Create("01234567890"));
    });
  }
}