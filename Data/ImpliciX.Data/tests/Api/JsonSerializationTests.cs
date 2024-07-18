using System;
using System.Text.Json;
using ImpliciX.Data.Api;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Data.Tests.Api;

public class JsonSerializationTests
{
    private static TestCaseData[] propertiesTestCaseDatas = {
       new (new FloatValue(1.5f), 1.5f),
       new (new LongValue(Int64.MinValue), Int64.MinValue),
       new (new ULongValue(UInt64.MaxValue), UInt64.MaxValue),
       new (new ShortValue(Int16.MinValue), Int16.MinValue),
       new (new UShortValue(UInt16.MaxValue), UInt16.MaxValue),
       new (new StringValue("foo"), "foo"),
       new (EnumValue.A, -1),
       new (new NotPublicValueOrEnum(42),string.Empty)
    };

    [TestCaseSource(nameof(propertiesTestCaseDatas))]
    public void properties_serialization(object modelValue, object expectedJsonValue)
    {
        var urn = "root:p";
        var message = new MessageProperties()
        {
            Properties = new []
            {
                new Property(CreateModelProperty(urn, modelValue))
            },
            At = "2021-01-01T00:00:00Z"
        };
        var json = message.ToJson();
        var jd = JsonDocument.Parse(json);
        Check.That(jd.RootElement.GetProperty("$type").GetString()).IsEqualTo("properties");
        Check.That(jd.RootElement.GetProperty("Properties").GetArrayLength()).IsEqualTo(1);
        var reloaded = Message.FromJson<MessageProperties>(json);
        Check.That(reloaded.Properties.Length).IsEqualTo(1);
        Check.That(reloaded.Properties[0].Urn).IsEqualTo(urn);
        Check.That(reloaded.Properties[0].Value).IsEqualTo(expectedJsonValue);
    }
    
    private IDataModelValue CreateModelProperty(string urn, object modelValue)
    {
        return modelValue switch 
        {
            FloatValue mv => Property<FloatValue>.Create(PropertyUrn<FloatValue>.Build(urn), mv, TimeSpan.Zero),
            LongValue mv => Property<LongValue>.Create(PropertyUrn<LongValue>.Build(urn), mv, TimeSpan.Zero),
            ULongValue mv => Property<ULongValue>.Create(PropertyUrn<ULongValue>.Build(urn), mv, TimeSpan.Zero),
            ShortValue mv => Property<ShortValue>.Create(PropertyUrn<ShortValue>.Build(urn), mv, TimeSpan.Zero),
            UShortValue mv => Property<UShortValue>.Create(PropertyUrn<UShortValue>.Build(urn), mv, TimeSpan.Zero),
            StringValue mv => Property<StringValue>.Create(PropertyUrn<StringValue>.Build(urn), mv, TimeSpan.Zero),
            EnumValue mv => Property<EnumValue>.Create(PropertyUrn<EnumValue>.Build(urn), mv, TimeSpan.Zero),
            NotPublicValueOrEnum mv => Property<NotPublicValueOrEnum>.Create(PropertyUrn<NotPublicValueOrEnum>.Build(urn), mv, TimeSpan.Zero),
            _ => throw new ArgumentException($"Unsupported model value type {modelValue.GetType()}")
        };
    }


    public class FloatValue: IPublicValue
    {
        private readonly float _v;

        public FloatValue(float v)
        {
            _v = v;
        }

        public object PublicValue()
        {
            return _v;
        }
    }

    public class ULongValue: IPublicValue
    {
        private readonly ulong _v;

        public ULongValue(ulong v)
        {
            _v = v;
        }

        public object PublicValue()
        {
            return _v;
        }
    }
    
    public class LongValue: IPublicValue
    {
        private readonly long _v;

        public LongValue(long v)
        {
            _v = v;
        }

        public object PublicValue()
        {
            return _v;
        }
    }
    public class ShortValue: IPublicValue
    {
        private readonly short _v;

        public ShortValue(short v)
        {
            _v = v;
        }

        public object PublicValue()
        {
            return _v;
        }
    }
    
    public class UShortValue: IPublicValue
    {
        private readonly ushort _v;

        public UShortValue(ushort v)
        {
            _v = v;
        }

        public object PublicValue()
        {
            return _v;
        }
    }
    public class StringValue:IPublicValue
    {
        private readonly string _v;

        public StringValue(string v)
        {
            _v = v;
        }
        public object PublicValue()
        {
            return _v;
        }
    }
    
    public enum EnumValue
    {
        A = -1,
        B = 0
    }
    
    public class NotPublicValueOrEnum
    {
        private readonly int _v;

        public NotPublicValueOrEnum(int v)
        {
            _v = v;
        }
    }
}