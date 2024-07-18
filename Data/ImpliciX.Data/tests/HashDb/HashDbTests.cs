using System;
using System.IO;
using System.Linq;
using System.Reflection;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.HashDb;

public class HashDbTests
{
  [SetUp]
  public void Init()
  {
    _folderPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
  }
  
  [TearDown]
  public void CleanUp()
  {
    Directory.Delete(_folderPath, true);
  }

  private string _folderPath;
  private readonly string _dbName = "theDb";
  
  [TestCase("root:temperature", "273.15")]
  [TestCase("root:temperature", "350")]
  [TestCase("root:presence", nameof(Presence.Enabled))]
  [TestCase("root:presence", nameof(Presence.Disabled))]
  public void ReadNonExisting(string key, string value)
  {
    using var db = CreateHashDb();
    var read = db.Read(key);
    Assert.That(read.IsError, Is.True);
  }
  
  [TestCase("root:temperature", "273.15")]
  [TestCase("root:temperature", "350")]
  [TestCase("root:presence", nameof(Presence.Enabled))]
  [TestCase("root:presence", nameof(Presence.Disabled))]
  public void WriteThenReadSingleNumeric(string key, string value)
  {
    var hv = new HashValue(key, value, TimeSpan.Zero);
    Write(hv);
    var read = Read(key);
    Assert.That(read.Value.ValuesWithoutAtField, Is.EqualTo(new [] {("value",value)}));
  }
  
  [Test]
  public void WriteThenReadFunctionDefinition()
  {
    var fields = new[] { ("a1", "1"), ("a2", "2"), ("a3", "3"), ("z", "0.68") };
    var hv = new HashValue("root:function", fields);
    Write(hv);
    var read = Read("root:function");
    Assert.That(read.Value.ValuesWithoutAtField, Is.EqualTo(fields));
  }
    
  [TestCase("root:temperature", "273.15", "350")]
  [TestCase("root:presence", nameof(Presence.Disabled), nameof(Presence.Enabled))]
  public void WriteTwiceThenRead(string key, string once, string twice)
  {
    Write(new HashValue(key, once, TimeSpan.Zero));
    Write(new HashValue(key, twice, TimeSpan.Zero));
    var read = Read(key);
    Assert.That(read.Value.ValuesWithoutAtField, Is.EqualTo(new [] {("value",twice)}));
  }

  [Test]
  public void FillThenReadAll()
  {
    var hvs = Fill();
    using var db = CreateHashDb();
    var read = db.ReadAll();
    Assert.That(read.IsSuccess, Is.True, () => read.Error.Message);
    Assert.That(read.Value.OrderBy(x => x.Key), Is.EqualTo(hvs.OrderBy(x => x.Key)));
  }
  
  [Test]
  public void FillThenDeleteAllThenReadSingle()
  {
    Fill();
    DeleteAll();
    using var db = CreateHashDb();
    var read = db.Read("root:temperature");
    Assert.That(read.IsError, Is.True);
  }
  
  [Test]
  public void FillThenDeleteAllThenReadAll()
  {
    Fill();
    DeleteAll();
    using var db = CreateHashDb();
    var read = db.ReadAll();
    Assert.That(read.IsSuccess, Is.True, () => read.Error.Message);
    Assert.That(read.Value, Is.Empty);
  }

  private void DeleteAll()
  {
    using var db = CreateHashDb();
    var del = db.DeleteAll();
    Assert.That(del.IsSuccess, Is.True, () => del.Error.Message);
  }

  private HashValue[] Fill()
  {
    var hv1 = new HashValue("root:temperature", "273.15", TimeSpan.Zero);
    Write(hv1);
    var hv2 = new HashValue("root:presence", nameof(Presence.Enabled), TimeSpan.Zero);
    Write(hv2);
    var hv3 = new HashValue("root:function", new[] { ("a1", "1"), ("a2", "2"), ("a3", "3"), ("z", "0.68") });
    Write(hv3);
    var hvs = new[] { hv1, hv2, hv3 };
    return hvs;
  }

  private Result<HashValue> Read(string key)
  {
    using var db = CreateHashDb();
    var read = db.Read(key);
    Assert.That(read.IsSuccess, Is.True, () => read.Error.Message);
    return read;
  }

  private void Write(HashValue hv)
  {
    using var db = CreateHashDb();
    var write = db.Write(hv);
    Assert.That(write.IsSuccess, Is.True, () => write.Error.Message);
  }

  private Data.HashDb.HashDb CreateHashDb()
  {
    var modelFactory = new ModelFactory(Assembly.GetExecutingAssembly());
    return new Data.HashDb.HashDb( modelFactory, _folderPath, _dbName);
  }
}