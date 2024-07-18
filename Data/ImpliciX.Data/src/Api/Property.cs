using System;
using System.Text.Json.Serialization;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Api;

public class Property
{
  public Property() { }
    
  public Property(IDataModelValue mv)
  {
    Urn = mv.Urn.Value;
    Value = ExtractSerializableValue(mv.ModelValue());
    var timeSpan = mv.At;
    At = Formaters.FormatTime(timeSpan);
  }

  public string Urn { get; set; }
  [JsonConverter(typeof(ObjectConverter))]
  public object Value { get; set; }
  public string At { get; set; }
    
  private static object ExtractSerializableValue(object value)
  {
    return value switch
    {
      IPublicValue p => p.PublicValue(),
      Enum e => Convert.ToInt32(e),
      _ => string.Empty
    };
  }
}

[Obsolete("Use PropertyValue instead")]
public class PropertyValueV1
{
  public PropertyValueV1() { }
    
  public PropertyValueV1(IDataModelValue mv)
  {
    Urn = mv.Urn.Value;
    Value = mv.ModelValue().ToString();
    var timeSpan = mv.At;
    At = Formaters.FormatTime(timeSpan);
  }

  public PropertyValueV1(Urn urn, string value, TimeSpan at)
  {
    Urn = urn;
    Value = value;
    At = Formaters.FormatTime(at);
  }

  public string Urn { get; set; }
  public string Value { get; set; }
  public string At { get; set; }
}