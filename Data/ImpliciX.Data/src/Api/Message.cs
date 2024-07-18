using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImpliciX.Data.Api;

[JsonDerivedType(typeof(MessagePrelude), "prelude")]
[JsonDerivedType(typeof(MessageProperties), "properties")]
[JsonDerivedType(typeof(MessageCommand), "command")]
[JsonDerivedType(typeof(MessageTimeSeries), "timeseries")]
public abstract class Message
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = {new JsonStringEnumConverter(), new ObjectConverter()}
    };

    [JsonConverter(typeof(StringEnumConverter<MessageKind>))]
    public MessageKind Kind { get; set; }

    public virtual string ToJson()
    {
        return JsonSerializer.Serialize(this, JsonSerializerOptions);
    }
    
    public static T FromJson<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonSerializerOptions);
    }

}


