using System;
using ImpliciX.Data.Factory;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.Factory;

public class HashValuesTests
{
    [Test]
    public void GivenSingleValueInsideHashValue_WhenIGetSingleValue_ThenIGetSome()
    {
        var sut = new HashValue("myKey", "myValue", TimeSpan.FromHours(2));
        var (value, at) = sut.GetSingleValue().GetValue();
        Check.That(value).IsEqualTo("myValue");
        Check.That(at).IsEqualTo(HashValue.TimeSpanAsString(TimeSpan.FromHours(2)));
    }

    private static object[] _getSingleValueNoneCases =
    {
        new object[] {new[] {(HashValue.SingleValueFieldName, "myValue1")}},
        new object[] {new[] {(HashValue.TimeSpanFieldName, "myAt1")}},
        new object[]
        {
            new[]
            {
                (HashValue.SingleValueFieldName, "myValue1"), (HashValue.TimeSpanFieldName, "myAt1"),
                (HashValue.SingleValueFieldName, "myValue2"), (HashValue.TimeSpanFieldName, "myAt2")
            }
        },
        new object[]
        {
            new[]
            {
                (HashValue.SingleValueFieldName, "myValue1"),
                (HashValue.SingleValueFieldName, "myValue2"), (HashValue.TimeSpanFieldName, "myAt2")
            }
        },
        new object[]
        {
            new[]
            {
                (HashValue.SingleValueFieldName, "myValue1"), (HashValue.TimeSpanFieldName, "myAt1"),
                (HashValue.SingleValueFieldName, "myValue2")
            }
        },
        new object[]
        {
            new[]
            {
                (HashValue.TimeSpanFieldName, "myAt1"),
                (HashValue.SingleValueFieldName, "myValue2"), (HashValue.TimeSpanFieldName, "myAt2")
            }
        },
        new object[]
        {
            new[]
            {
                (HashValue.SingleValueFieldName, "myValue1"), (HashValue.TimeSpanFieldName, "myAt1"),
                (HashValue.TimeSpanFieldName, "myAt2")
            }
        }
    };

    [TestCaseSource(nameof(_getSingleValueNoneCases))]
    public void GivenWrongValuesCountInsideHashValue_WhenIGetSingleValue_ThenIGetNone((string Name, string Value)[] allValues)
    {
        var sut = new HashValue("myKey", allValues);
        Check.That(sut.GetSingleValue().IsNone).IsTrue();
    }
}