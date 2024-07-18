using ImpliciX.Language.Records;
using ImpliciX.ReferenceApp.Model;
using static ImpliciX.Language.Records.Records;

namespace ImpliciX.ReferenceApp.App;

internal static class AllRecords
{
  public static readonly IRecord[] Records =
  {
    Record(general.report_records_node)
      .Is.Last(3).Snapshot
      .Of(general.report_record_writer)
      .Instance
  };
}