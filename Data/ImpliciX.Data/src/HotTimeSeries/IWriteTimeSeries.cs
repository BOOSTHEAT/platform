using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.HotTimeSeries;

public interface IWriteTimeSeries
{
    Option<Unit> WriteMany(List<IDataModelValue> series, Dictionary<Urn, TimeSpan> seriesDefinitions);
    Option<Unit> Write(string key, TimeSpan at, double value);
    long Delete(string key, TimeSpan? from = null, TimeSpan? to = null);
    Result<Unit> SetupTimeSeries(string key, TimeSpan retentionPeriod);
    Option<Unit> DeleteOlderThan(string key, TimeSpan period);
    void ApplyRetentionPolicy();
}