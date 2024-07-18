using System;
using System.Collections.Generic;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.Records.HotRecords;

public interface IHotRecordsDb : IDisposable
{
    void Write(Snapshot snapshot);
    IReadOnlyList<Snapshot> ReadAll(Urn recordUrn);
}