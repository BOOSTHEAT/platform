using System;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.Factory;

public class ModelFactoryTests
{
    [Test]
    public void WhenFindUrnType_WithUrnKnownInAssembly_ThenIGetTypeExpected()
    {
        var sut = CreateSut();
        var result = sut.FindUrnType(dummy.app1.update_progress);
        Check.That(result.IsSuccess).IsTrue();
        Check.That(result.Value).IsEqualTo(dummy.app1.update_progress.GetType());
    }

    [Test]
    public void WhenFindUrnType_WithUrnUnknownInAssembly_ThenIGetAnError()
    {
        var sut = CreateSut();
        var result = sut.FindUrnType(Urn.BuildUrn(dummy.app1.Urn, "unknownProperty"));
        Check.That(result.IsError).IsTrue();
    }

    [Test]
    public void WhenCreateUrn_WithUrnKnownInAssembly_ThenIGetTypeExpected()
    {
        var sut = CreateSut();
        var result = sut.Create(dummy.app1.update_progress, "4.2", TimeSpan.FromMinutes(12));

        Check.That(result.IsSuccess).IsTrue();
        var value = (Property<Percentage>) result.Value;
        Check.That(value.Urn).IsEqualTo(dummy.app1.update_progress);
        Check.That(value.Value.ToFloat()).IsEqualTo(4.2f);
        Check.That(value.At).IsEqualTo(TimeSpan.FromMinutes(12));
    }

    [Test]
    public void WhenCreateUrn_WithStringOfAnUrnKnownInAssembly_ThenIGetTypeExpected()
    {
        var sut = CreateSut();
        var result = sut.Create(dummy.app1.update_progress.Value, "4.2", TimeSpan.FromMinutes(12));

        Check.That(result.IsSuccess).IsTrue();
        var value = (Property<Percentage>) result.Value;
        Check.That(value.Urn).IsEqualTo(dummy.app1.update_progress);
        Check.That(value.Value.ToFloat()).IsEqualTo(4.2f);
        Check.That(value.At).IsEqualTo(TimeSpan.FromMinutes(12));
    }

    [Test]
    public void WhenCreateUrn_WithObjectWhichIsNotAnUrnOrAString_ThenIGetAnError()
    {
        var sut = CreateSut();
        var result = sut.Create(new object(), "4.2", TimeSpan.FromMinutes(12));
        Check.That(result.IsError).IsTrue();
        Check.That(result.Error.Message).Contains("Could not create an valid urn");
    }

    [Test]
    public void WhenCreateUrn_WithUrnUnknownInAssembly_ThenIGetAnError()
    {
        var sut = CreateSut();
        var result = sut.Create(Urn.BuildUrn(dummy.app1.Urn, "unknownProperty"), "4.2", TimeSpan.FromMinutes(12));
        Check.That(result.IsError).IsTrue();
    }

    private ModelFactory CreateSut() => new (GetType().Assembly);
}