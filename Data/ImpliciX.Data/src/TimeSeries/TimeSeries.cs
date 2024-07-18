using ImpliciX.Language.Model;

namespace ImpliciX.Data.TimeSeries;

public class TimeSeries : ITimeSeries
{
  public TimeSeries(Urn urn, params Urn[] fields)
  {
    Urn = urn;
    Fields = fields;
  }

  public Urn Urn { get; }
  public Urn[] Fields { get; }
}