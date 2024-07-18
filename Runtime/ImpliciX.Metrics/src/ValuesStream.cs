using System;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.Metrics
{
    /// <summary>
    /// Responsibility:
    /// - Give stream read/write access according retention policy
    /// </summary>
    internal sealed class ValuesStream
    {
        public string Key { get; }
        private readonly IReadTimeSeries _tsReader;
        private readonly IWriteTimeSeries _tsWriter;
        private readonly TimeSpan _retention;
        private readonly Func<TimeSpan>? _getOldestAtBoundary;

        private ValuesStream(string key, IReadTimeSeries tsReader, IWriteTimeSeries tsWriter, TimeSpan retention, Func<TimeSpan>? getOldestAtBoundary)
        {
            Key = key;
            _tsReader = tsReader ?? throw new ArgumentNullException(nameof(tsReader));
            _tsWriter = tsWriter ?? throw new ArgumentNullException(nameof(tsWriter));
            _retention = retention;
            _getOldestAtBoundary = getOldestAtBoundary;
        }

        public static ValuesStream Create(string streamId, IReadTimeSeries dataReader, IWriteTimeSeries dataWriter, TimeSpan period,
            Func<TimeSpan>? getOldestAtBound = null)
            => new ValuesStream(streamId, dataReader, dataWriter, period, getOldestAtBound).Setup();

        private ValuesStream Setup()
        {
            _tsWriter.SetupTimeSeries(Key, Retention.NotManagedByStorage);
            return this;
        }

        public Option<DataModelValue<float>> GetAt(TimeSpan at)
        {
            return
                from _ in ApplyRetention()
                from it in _tsReader.ReadAt(Key, at.Ticks)
                select it;
        }

        public Option<DataModelValue<float>> First(TimeSpan? fromAt = null)
        {
            return
                from _ in ApplyRetention()
                from first in _tsReader.ReadFirst(Key, fromAt?.Ticks)
                select first;
        }

        public Option<DataModelValue<float>> Last(TimeSpan? upTo = null)
        {
            var readLastUpTo = upTo?.Ticks ?? GetReadLastUpTo()?.Ticks;
            return
                from _ in ApplyRetention()
                from last in _tsReader.ReadLast(Key, readLastUpTo)
                select last;
        }

        private TimeSpan? GetReadLastUpTo()
        {
            if (_getOldestAtBoundary is null) return null;

            var periodEndTime = _getOldestAtBoundary() + _retention;
            if (periodEndTime <= TimeSpan.Zero) return null;

            var upToThatExcludeItemOnOrAfterPeriodEnd = new TimeSpan(periodEndTime.Ticks - 1);
            return upToThatExcludeItemOnOrAfterPeriodEnd;
        }

        public Option<DataModelValue<float>[]> All(TimeSpan upTo)
        {
            return from _ in ApplyRetention()
                from values in _tsReader.ReadAll(Key, upTo: upTo.Ticks)
                select values;
        }

        public Option<DataModelValue<float>[]> ReadAllPeriodValues()
            => All(GetReadLastUpTo() ?? TimeSpan.MaxValue);

        public Option<Unit> Write(float value, TimeSpan at)
        {
            return from _ in _tsWriter.Write(Key, at, value)
                from __ in ApplyRetention()
                select default(Unit);
        }

        private Option<Unit> DeleteOlderThanDate(TimeSpan periodStartTime)
        {
            _tsWriter.Delete(Key, TimeSpan.Zero, new TimeSpan(periodStartTime.Ticks - 1));
            return default(Unit);
        }

        private Option<Unit> ApplyRetention()
        {
            return _getOldestAtBoundary is null
                ? _tsWriter.DeleteOlderThan(Key, _retention)
                : DeleteOlderThanDate(_getOldestAtBoundary());
        }
    }
}