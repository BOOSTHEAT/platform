using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Data.HashDb;
using ImpliciX.Language.Core;
using Moq;
using NUnit.Framework;

namespace ImpliciX.PersistentStore.Tests;

[TestFixtureSource(nameof(_operations))]
public class LocalStorageTests
{
  private static TestCaseData[] _operations = new[]
  {
    Operation(CheckRead),
    Operation(CheckReadAll),
    Operation(CheckWrite),
    Operation(CheckFlush),
    Operation(CheckSubscription),
  };

  private static TestCaseData Operation(Func<LocalStorage, int, Mock<IHashDb>, Result<Unit>, Result<Unit>> op)
  {
    var tcd = new TestCaseData(op);
    return tcd;
  }
  
  private static Result<Unit> CheckRead(LocalStorage sut, int id, Mock<IHashDb> hashDbMock, Result<Unit> mockReturns)
  {
    var hash = new HashValue("foo", "bar", TimeSpan.Zero);
    hashDbMock?.Setup(x => x.Read("foo")).Returns(mockReturns.Select(_ => hash));
    var result = sut.ReadHash(id, "foo");
    hashDbMock?.Verify(x => x.Read("foo"));
    if(mockReturns!=null && mockReturns.IsSuccess)
      Assert.That(result, Is.EqualTo(hash));
    return result.Values.Any() ? Success : Error;
  }
  
  private static Result<Unit> CheckReadAll(LocalStorage sut, int id, Mock<IHashDb> hashDbMock, Result<Unit> mockReturns)
  {
    var hashes = new HashValue[]
    {
      new ("foo", "bar", TimeSpan.Zero),
      new ("fizz", "buzz", TimeSpan.Zero),
    };
    hashDbMock?.Setup(x => x.ReadAll()).Returns(mockReturns.Select(_ => hashes.Cast<HashValue>()));
    var result = sut.ReadAll(id).ToArray();
    hashDbMock?.Verify(x => x.ReadAll());
    if(mockReturns!=null && mockReturns.IsSuccess)
      Assert.That(result, Is.EqualTo(hashes));
    return result.Any() ? Success : Error;
  }
  
  private static Result<Unit> CheckWrite(LocalStorage sut, int id, Mock<IHashDb> hashDbMock, Result<Unit> mockReturns)
  {
    var hash = new HashValue("foo", "bar", TimeSpan.Zero);
    hashDbMock?.Setup(x => x.Write(hash)).Returns(mockReturns);
    var result = sut.WriteHash(id, hash);
    hashDbMock?.Verify(x => x.Write(hash));
    return result;
  }
  
  private static Result<Unit> CheckFlush(LocalStorage sut, int id, Mock<IHashDb> hashDbMock, Result<Unit> mockReturns)
  {
    hashDbMock?.Setup(x => x.DeleteAll()).Returns(mockReturns);
    var result = sut.FlushDb(id);
    hashDbMock?.Verify(x => x.DeleteAll());
    return result;
  }
  
  private static Result<Unit> CheckSubscription(LocalStorage sut, int id, Mock<IHashDb> hashDbMock, Result<Unit> mockReturns)
  {
    var hash = new HashValue("foo", "bar", TimeSpan.Zero);
    hashDbMock?.Setup(x => x.Write(hash)).Returns(mockReturns);
    var actualKeyChange = string.Empty;
    sut.Listener.SubscribeAllKeysModification(id, changedKey => actualKeyChange = changedKey );
    var result = sut.WriteHash(id, hash);
    Assert.That(actualKeyChange, Is.EqualTo(result.IsSuccess ? "foo" : ""));
    return result;
  }

  public LocalStorageTests(TestCaseData data)
  {
    _operation = (Func<LocalStorage,int,Mock<IHashDb>,Result<Unit>,Result<Unit>>) data.Arguments[0];
  }

  private readonly Func<LocalStorage, int, Mock<IHashDb>, Result<Unit>, Result<Unit>> _operation;
  
  private static TestCaseData[] _cases = new[]
  {
    new TestCaseData(1, "user"),
    new TestCaseData(2, "version"),
    new TestCaseData(3, "factory"),
  };

  [TestCaseSource(nameof(_cases))]
  public void Successful(int id, string name)
  {
    var sut = new LocalStorage(n => _dbs[n].Object);
    var result = _operation(sut, id, _dbs[name], Success);
    Assert.True(result.IsSuccess);
  }

  [TestCaseSource(nameof(_cases))]
  public void Failed(int id, string name)
  {
    var sut = new LocalStorage(n => _dbs[n].Object);
    var result = _operation(sut, id, _dbs[name], Error);
    Assert.True(result.IsError);
  }

  [Test]
  public void UnknownDb()
  {
    var sut = new LocalStorage(n => _dbs[n].Object);
    var result = _operation(sut, 8, null, null);
    Assert.True(result.IsError);
  }

  private Dictionary<string, Mock<IHashDb>> _dbs = new Dictionary<string, Mock<IHashDb>>
  {
    ["user"] = new(),
    ["version"] = new(),
    ["factory"] = new(),
  };

  private static readonly Result<Unit> Success = Result<Unit>.Create(new Unit());
  private static readonly Result<Unit> Error = Result<Unit>.Create(new Error("what","ever"));
}