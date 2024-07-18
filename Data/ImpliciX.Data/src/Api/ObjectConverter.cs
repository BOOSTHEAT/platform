using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImpliciX.Data.Api;

public class ObjectConverter:JsonConverter<object>  
{
    public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        return jsonDoc.RootElement.ValueKind switch
        {
            JsonValueKind.String => jsonDoc.RootElement.GetString(),
            JsonValueKind.Number => jsonDoc.RootElement.GetSingle(),
            JsonValueKind.Null => null,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case float f:
                writer.WriteNumberValue(f);
                break;
            case short s:
                writer.WriteNumberValue(s);
                break;
            case ushort us:
                writer.WriteNumberValue(us);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case uint i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case ulong l:
                writer.WriteNumberValue(l);
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}