using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Data.HotTimeSeries;

public record TimeSeriesInfo(long SampleCount)
{
    public long SampleCount { get; } = SampleCount;
}

public interface IReadTimeSeries
{
    Option<Urn[]> AllKeys(string pattern = null);
    Option<DataModelValue<float>[]> ReadMany(IEnumerable<Urn> keys, long from, long to, long? count = null);
    Option<DataModelValue<float>> ReadAt(string key, long at);
    Option<DataModelValue<float>> ReadFirst(string key, long? from);
    Option<DataModelValue<float>> ReadLast(string key, long? upTo = null);
    Option<DataModelValue<float>[]> ReadAll(string key, long? count = null, long? upTo = null);
    void ApplyRetentionPolicy();
}