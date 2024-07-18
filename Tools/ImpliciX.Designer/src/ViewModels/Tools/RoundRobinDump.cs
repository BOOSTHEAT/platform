using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ImpliciX.Designer.ViewModels.Tools
{
  public class RoundRobinDump
  {
    public RoundRobinDump(XDocument rrd)
    {
      _step = int.Parse(rrd.XPathSelectElement("rrd/step").Value);
      _lastUpdate = int.Parse(rrd.XPathSelectElement("rrd/lastupdate").Value);
      _fields = rrd.XPathSelectElements("rrd/ds/name").Select(e => e.Value.Trim()).ToArray();
      _archives = rrd.XPathSelectElements("rrd/rra");
    }

    public Dictionary<SerieIdentifier, IEnumerable<(DateTime, float)>> Load()
    {
      var result = new Dictionary<SerieIdentifier, IEnumerable<(DateTime, float)>>();
      foreach (var rra in _archives)
      {
        var (function, window, rows) = LoadArchive(rra);
        foreach (var (field,index) in _fields.Select((f,i) => (f,i)))
        {
          var serieId = new SerieIdentifier(field, function, window);
          var points = rows.Select(x => (x.time, x.values[index])).ToArray();
          result[serieId] = points;
        }
      }
      return result;
    }

    private (string function, int window, (DateTime time, float[] values)[]) LoadArchive(XElement rra)
    {
      var function = rra.XPathSelectElement("cf").Value;
      var multiplier = int.Parse(rra.XPathSelectElement("pdp_per_row").Value);
      var window = multiplier * _step;
      var rows = rra.XPathSelectElements("database/row").ToArray();
      var lastValueTime = _lastUpdate - _lastUpdate % window;
      var firstValueTime = lastValueTime - window * (rows.Length - 1);
      var startAt = Epoch.AddSeconds(firstValueTime);
      var tRows = rows.Select((e, i) => (
        startAt.AddSeconds(i * window),
        e.XPathSelectElements("v").Select(ev => float.Parse(ev.Value, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray()
        )).ToArray();
      return (function, window, tRows);
    }

    public struct SerieIdentifier
    {
      public readonly string Name;
      public readonly string Function;
      public readonly int Window;

      public SerieIdentifier(string name, string function, int window)
      {
        Name = name;
        Function = function;
        Window = window;
      }
    }

    private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly int _step;
    private readonly int _lastUpdate;
    private readonly string[] _fields;
    private readonly IEnumerable<XElement> _archives;
  }
}