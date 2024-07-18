using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ImpliciX.DesktopServices.Helpers;

internal sealed class StringEnumConverter<T> : JsonConverter<T>
{
    private readonly JsonConverter<T> _converter;
    private readonly Type _underlyingType;
    
    public StringEnumConverter() : this(null)
    {
    }
    
    public StringEnumConverter(JsonSerializerOptions options)
    {
        if (options != null)
        {
            _converter = (JsonConverter<T>) options.GetConverter(typeof(T));
        }
    
        _underlyingType = typeof(T);
    }
    
    public override bool CanConvert(Type typeToConvert)
    {
        return typeof(T).IsAssignableFrom(typeToConvert);
    }
    
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (_converter != null)
        {
            return _converter.Read(ref reader, _underlyingType, options);
        }
    
        var value = reader.GetString();
        if (string.IsNullOrEmpty(value) || 
            !Enum.TryParse(_underlyingType, value, ignoreCase: false, out var result) &&
            !Enum.TryParse(_underlyingType, value, ignoreCase: true, out result))
        {
            throw new JsonException($"Unable to convert \"{value}\" to Enum \"{_underlyingType}\".");
        }
    
        return (T) result;
    }
    
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}