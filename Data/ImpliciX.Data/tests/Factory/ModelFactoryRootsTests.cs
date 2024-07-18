using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.Language.StdLib;
using NFluent;
using NUnit.Framework;
using Enumerable = System.Linq.Enumerable;

namespace ImpliciX.Data.Tests.Factory;

public class ModelFactoryRootsTests
{
  [Test]
  public void UrnIsInAllUrns()
  {
    var allUrns = Enumerable.ToArray(CreateSut().GetAllUrns());
    Check.That(allUrns).Contains(my_device._.Urn);
    Check.That(allUrns).Contains(my_device._.app.Urn);
    Check.That(allUrns).Contains(my_device._.app.update_progress);
    Check.That(allUrns).Contains(my_device._.fubr.Urn);
    Check.That(allUrns).Contains(my_device._.fubr.update_progress);
    Check.That(allUrns).Contains(my_device._.fubr.presence);
  }
  
  [Test]
  public void WhenFindUrnType_WithStdDeviceUrnInAssembly_ThenIGetTypeExpected() =>
    WhenFindUrnType_ThenIGetTypeExpected(my_device._.fubr.update_progress, my_device._.fubr.update_progress.GetType());
  
  [Test]
  public void WhenFindStrUrnType_WithStdDeviceUrnInAssembly_ThenIGetTypeExpected() =>
    WhenFindUrnType_ThenIGetTypeExpected("my_device:fubr:update_progress", my_device._.fubr.update_progress.GetType());

  [Test]
  public void WhenCreateUrn_WithStdDeviceUrnInAssembly_ThenIGetTypeExpected() =>
    WhenCreateUrn_ThenIGet(my_device._.fubr.update_progress.Value, "4.2", my_device._.fubr.update_progress, 4.2f);

  [Test]
  public void WhenCreateStrUrn_WithStdDeviceUrnInAssembly_ThenIGetTypeExpected() =>
    WhenCreateUrn_ThenIGet("my_device:fubr:update_progress", "4.2", my_device._.fubr.update_progress, 4.2f);

  public void WhenFindUrnType_ThenIGetTypeExpected(object urnCandidate, Type expectedType)
  {
    var sut = CreateSut();
    var result = sut.FindUrnType(urnCandidate);
    Check.That(result.IsSuccess).IsTrue();
    Check.That(result.Value).IsEqualTo(expectedType);
  }
  
  public void WhenCreateUrn_ThenIGet(object urnCandidate, object valueCandidate, object expectedUrn, float expectedValue)
  {
    var sut = CreateSut();
    var result = sut.Create(urnCandidate, valueCandidate, TimeSpan.FromMinutes(12));
    Check.That(result.IsSuccess).IsTrue();
    var value = (IDataModelValue) result.Value;
    Check.That(value.Urn).IsEqualTo(expectedUrn);
    Check.That(((IFloat) value.ModelValue()).ToFloat()).IsEqualTo(expectedValue);
    Check.That(value.At).IsEqualTo(TimeSpan.FromMinutes(12));
  }

  private ModelFactory CreateSut() => new (GetType().Assembly);
}

public class my_device : Device
{
  public static my_device _ { get; }= new ();

  private my_device() : base(nameof(my_device))
  {
    fubr = new HardwareAndSoftwareDeviceNode(nameof(fubr), this);
  }
  public HardwareAndSoftwareDeviceNode fubr { get; }
}