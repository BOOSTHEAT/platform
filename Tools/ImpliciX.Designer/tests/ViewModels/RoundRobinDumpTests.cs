using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using ImpliciX.Designer.ViewModels.Tools;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Designer.Tests.ViewModels
{
  public class RoundRobinDumpTests
  {
    [TestCaseSource(nameof(RrdCases))]
    public void check_rrd(string id, string rrd,
      Dictionary<RoundRobinDump.SerieIdentifier, IEnumerable<(DateTime,float)>> expected)
    {
      var doc = XDocument.Parse(rrd);
      var data = new RoundRobinDump(doc).Load();
      Check.That(data).IsEqualTo(expected);
    }

    static object[] RrdCases =
    {
      new[]
      {
        "single_serie", @"
        <rrd>
          <step>10</step> <!-- Seconds -->
          <lastupdate>1646932389</lastupdate> <!-- 2022-03-10 17:13:09 UTC -->
          <ds>
          <name> value </name>
          </ds>
          <rra>
            <cf>AVERAGE</cf>
            <pdp_per_row>1</pdp_per_row> <!-- 10 seconds -->
            <database>
			        <!-- 2022-03-10 17:12:40 UTC / 1646932360 --> <row><v>1.0574085698e+00</v></row>
			        <!-- 2022-03-10 17:12:50 UTC / 1646932370 --> <row><v>1.6843429414e+00</v></row>
			        <!-- 2022-03-10 17:13:00 UTC / 1646932380 --> <row><v>3.8958296011e+00</v></row>
		        </database>
	        </rra>
        </rrd>",
        Expected(
          Serie("value", "AVERAGE", 10,
            ("2022-03-10T17:12:40Z", 1.0574085698f),
            ("2022-03-10T17:12:50Z", 1.6843429414f),
            ("2022-03-10T17:13:00Z", 3.8958296011f))
        )
      },
      new[]
      {
        "other_single_serie", @"
        <rrd>
          <step>10</step> <!-- Seconds -->
          <lastupdate>1646932389</lastupdate> <!-- 2022-03-10 17:13:09 UTC -->
          <ds>
          <name> value </name>
          </ds>
          <rra>
            <cf>AVERAGE</cf>
            <pdp_per_row>1</pdp_per_row> <!-- 10 seconds -->
            <database>
			        <!-- 2022-03-10 17:12:30 UTC / 1646932350 --> <row><v>1.2037702161e+00</v></row>
			        <!-- 2022-03-10 17:12:40 UTC / 1646932360 --> <row><v>1.0574085698e+00</v></row>
			        <!-- 2022-03-10 17:12:50 UTC / 1646932370 --> <row><v>1.6843429414e+00</v></row>
			        <!-- 2022-03-10 17:13:00 UTC / 1646932380 --> <row><v>3.8958296011e+00</v></row>
		        </database>
	        </rra>
        </rrd>",
        Expected(
          Serie("value", "AVERAGE", 10,
            ("2022-03-10T17:12:30Z", 1.2037702161f),
            ("2022-03-10T17:12:40Z", 1.0574085698f),
            ("2022-03-10T17:12:50Z", 1.6843429414f),
            ("2022-03-10T17:13:00Z", 3.8958296011f))
        )
      },
      new[]
      {
        "multiple_series_by_function", @"
        <rrd>
          <step>10</step> <!-- Seconds -->
          <lastupdate>1646932389</lastupdate> <!-- 2022-03-10 17:13:09 UTC -->
          <ds>
          <name> value </name>
          </ds>
          <rra>
            <cf>AVERAGE</cf>
            <pdp_per_row>1</pdp_per_row> <!-- 10 seconds -->
            <database>
			        <!-- 2022-03-10 17:12:30 UTC / 1646932350 --> <row><v>1.2037702161e+00</v></row>
			        <!-- 2022-03-10 17:12:40 UTC / 1646932360 --> <row><v>1.0574085698e+00</v></row>
			        <!-- 2022-03-10 17:12:50 UTC / 1646932370 --> <row><v>1.6843429414e+00</v></row>
			        <!-- 2022-03-10 17:13:00 UTC / 1646932380 --> <row><v>3.8958296011e+00</v></row>
		        </database>
	        </rra>
          <rra>
            <cf>MIN</cf>
            <pdp_per_row>1</pdp_per_row> <!-- 10 seconds -->
            <database>
			          <!-- 2022-03-10 17:12:30 UTC / 1646932350 --> <row><v>0.2037702161e+00</v></row>
			          <!-- 2022-03-10 17:12:40 UTC / 1646932360 --> <row><v>0.0574085698e+00</v></row>
			          <!-- 2022-03-10 17:12:50 UTC / 1646932370 --> <row><v>0.6843429414e+00</v></row>
			          <!-- 2022-03-10 17:13:00 UTC / 1646932380 --> <row><v>2.8958296011e+00</v></row>
		        </database>
	        </rra>
        </rrd>",
        Expected(
          Serie("value", "AVERAGE", 10,
            ("2022-03-10T17:12:30Z", 1.2037702161f),
            ("2022-03-10T17:12:40Z", 1.0574085698f),
            ("2022-03-10T17:12:50Z", 1.6843429414f),
            ("2022-03-10T17:13:00Z", 3.8958296011f)),
          Serie("value", "MIN", 10,
            ("2022-03-10T17:12:30Z", 0.2037702161f),
            ("2022-03-10T17:12:40Z", 0.0574085698f),
            ("2022-03-10T17:12:50Z", 0.6843429414f),
            ("2022-03-10T17:13:00Z", 2.8958296011f))
        )
      },
      new[]
      {
        "multiple_series_by_window", @"
        <rrd>
          <step>10</step> <!-- Seconds -->
          <lastupdate>1646932389</lastupdate> <!-- 2022-03-10 17:13:09 UTC -->
          <ds>
          <name> value </name>
          </ds>
          <rra>
            <cf>AVERAGE</cf>
            <pdp_per_row>1</pdp_per_row> <!-- 10 seconds -->
            <database>
			        <!-- 2022-03-10 17:12:30 UTC / 1646932350 --> <row><v>1.2037702161e+00</v></row>
			        <!-- 2022-03-10 17:12:40 UTC / 1646932360 --> <row><v>1.0574085698e+00</v></row>
			        <!-- 2022-03-10 17:12:50 UTC / 1646932370 --> <row><v>1.6843429414e+00</v></row>
			        <!-- 2022-03-10 17:13:00 UTC / 1646932380 --> <row><v>3.8958296011e+00</v></row>
		        </database>
	        </rra>
          <rra>
            <cf>AVERAGE</cf>
            <pdp_per_row>223</pdp_per_row> <!-- 2230 seconds -->
            <database>
			        <!-- 2022-03-10 14:55:30 UTC / 1646924130 --> <row><v>1.1930616940e+00</v></row>
			        <!-- 2022-03-10 15:32:40 UTC / 1646926360 --> <row><v>1.2268309686e+00</v></row>
			        <!-- 2022-03-10 16:09:50 UTC / 1646928590 --> <row><v>1.1901253040e+00</v></row>
			        <!-- 2022-03-10 16:47:00 UTC / 1646930820 --> <row><v>1.2072099909e+00</v></row>
		        </database>
	        </rra>
        </rrd>",
        Expected(
          Serie("value", "AVERAGE", 10,
            ("2022-03-10T17:12:30Z", 1.2037702161f),
            ("2022-03-10T17:12:40Z", 1.0574085698f),
            ("2022-03-10T17:12:50Z", 1.6843429414f),
            ("2022-03-10T17:13:00Z", 3.8958296011f)),
          Serie("value", "AVERAGE", 2230,
            ("2022-03-10T14:55:30Z", 1.1930616940f),
            ("2022-03-10T15:32:40Z", 1.2268309686f),
            ("2022-03-10T16:09:50Z", 1.1901253040f),
            ("2022-03-10T16:47:00Z", 1.2072099909f))
        )
      },
      new[]
      {
        "multiple_series_by_name", @"
        <rrd>
          <step>10</step> <!-- Seconds -->
          <lastupdate>1646932389</lastupdate> <!-- 2022-03-10 17:13:09 UTC -->
          <ds>
            <name> user </name>
          </ds>
          <ds>
            <name> syst </name>
          </ds>
          <rra>
            <cf>AVERAGE</cf>
            <pdp_per_row>1</pdp_per_row> <!-- 10 seconds -->
            <database>
			        <!-- 2022-03-10 17:12:30 UTC / 1646932350 --> <row><v>1.0960000000e+05</v><v>4.6000000000e+03</v></row>
			        <!-- 2022-03-10 17:12:40 UTC / 1646932360 --> <row><v>1.0600000000e+05</v><v>1.0000000000e+03</v></row>
			        <!-- 2022-03-10 17:12:50 UTC / 1646932370 --> <row><v>1.0610000000e+05</v><v>1.0000000000e+03</v></row>
			        <!-- 2022-03-10 17:13:00 UTC / 1646932380 --> <row><v>1.0700000000e+05</v><v>1.1000000000e+03</v></row>
		        </database>
	        </rra>
        </rrd>",
        Expected(
          Serie("user", "AVERAGE", 10,
            ("2022-03-10T17:12:30Z", 109600f),
            ("2022-03-10T17:12:40Z", 106000f),
            ("2022-03-10T17:12:50Z", 106100f),
            ("2022-03-10T17:13:00Z", 107000f)),
          Serie("syst", "AVERAGE", 10,
            ("2022-03-10T17:12:30Z", 4600f),
            ("2022-03-10T17:12:40Z", 1000f),
            ("2022-03-10T17:12:50Z", 1000f),
            ("2022-03-10T17:13:00Z", 1100f))
        )
      },
      new[]
      {
        "NaN", @"
        <rrd>
          <step>10</step> <!-- Seconds -->
          <lastupdate>1646932389</lastupdate> <!-- 2022-03-10 17:13:09 UTC -->
          <ds>
          <name> value </name>
          </ds>
          <rra>
            <cf>AVERAGE</cf>
            <pdp_per_row>1</pdp_per_row> <!-- 10 seconds -->
            <database>
			        <!-- 2022-03-10 17:12:40 UTC / 1646932360 --> <row><v>1.0574085698e+00</v></row>
			        <!-- 2022-03-10 17:12:50 UTC / 1646932370 --> <row><v>NaN</v></row>
			        <!-- 2022-03-10 17:13:00 UTC / 1646932380 --> <row><v>3.8958296011e+00</v></row>
		        </database>
	        </rra>
        </rrd>",
        Expected(
          Serie("value", "AVERAGE", 10,
            ("2022-03-10T17:12:40Z", 1.0574085698f),
            ("2022-03-10T17:12:50Z", float.NaN),
            ("2022-03-10T17:13:00Z", 3.8958296011f))
        )
      }
    };

    private static object Expected(
      params KeyValuePair<RoundRobinDump.SerieIdentifier, IEnumerable<(DateTime,float)>>[] data) =>
      new Dictionary<RoundRobinDump.SerieIdentifier, IEnumerable<(DateTime,float)>>(data);

    private static KeyValuePair<RoundRobinDump.SerieIdentifier, IEnumerable<(DateTime,float)>> Serie(string name,
      string function, int window, params (string,float)[] data) =>
      new KeyValuePair<RoundRobinDump.SerieIdentifier, IEnumerable<(DateTime,float)>>(
        new RoundRobinDump.SerieIdentifier(name, function, window),
        data.Select(d => (DateTime.Parse(d.Item1).ToUniversalTime(), d.Item2))
      );
  }
}