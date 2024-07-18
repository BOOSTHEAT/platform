#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotDb.Model;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Data.HotDb;
using Db = ImpliciX.Data.HotDb.HotDb;
namespace ImpliciX.Data.HotTimeSeries
{
    public class TimeSeriesDb : IReadTimeSeries, IWriteTimeSeries, IDisposable
    {
        public string DbName { get; }
        private const int BlocksPerSegment = 100;
        private readonly IHotDb _hotDb;
       
        private ConcurrentDictionary<string, TimeSpan> _retentionPeriod = new();
        
        public IReadOnlyDictionary<string, HashSet<StructDef>> DefinedStructs => _hotDb.DefinedStructsByName;
        public IReadOnlyDictionary<string, TimeSpan> RetentionPeriods => _retentionPeriod;

        public TimeSeriesDb(string folderPath, string dbName, bool safeLoad = false)
        {
            if (folderPath == null) throw new ArgumentNullException(nameof(folderPath));
            DbName = dbName ?? throw new ArgumentNullException(nameof(dbName));

            _hotDb = Directory.Exists(folderPath) && Directory.EnumerateFiles(folderPath).Any()
                ? Db.Load(folderPath, dbName, safeLoad)
                : Db.Create(folderPath, dbName);
        }

        public Option<Urn[]> AllKeys(string? pattern = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            return _hotDb.DefinedStructsByName.Keys.Select(it => Urn.BuildUrn(it)).ToArray();
        }

        public Option<DataModelValue<float>[]> ReadMany(IEnumerable<Urn> keys, long from, long to, long? count = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            return keys
                .Where(key => _hotDb.IsDefined(key))
                .Select(key => ReadAll(key.Value, count, to))
                .Where(it => it.IsSome)
                .SelectMany(it => it.GetValue())
                .ToArray();
        }

        public Option<DataModelValue<float>> ReadAt(string key, long at)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            if (!_hotDb.IsDefined(key)) return Option<DataModelValue<float>>.None();
            var bytes = _hotDb.Get(key, at);
            if (bytes.Length == 0) return Option<DataModelValue<float>>.None();
            return Option<DataModelValue<float>>.Some(TimeSeriesDbExt.FromBytes(bytes, key));
        }

        public Option<DataModelValue<float>> ReadFirst(string key, long? from = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            if (!_hotDb.IsDefined(key)) return Option<DataModelValue<float>>.None();

            byte[] b = _hotDb.GetFirst(key, from);
            if (b.Length == 0)
                return Option<DataModelValue<float>>.None();

            return Option<DataModelValue<float>>.Some(TimeSeriesDbExt.FromBytes(b, key));
        }

        public Option<DataModelValue<float>> ReadLast(string key, long? upTo = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            if (!_hotDb.IsDefined(key)) return Option<DataModelValue<float>>.None();
            byte[] b = _hotDb.GetLast(key, upTo);
            if (b.Length == 0)
                return Option<DataModelValue<float>>.None();

            return Option<DataModelValue<float>>.Some(TimeSeriesDbExt.FromBytes(b, key));
        }

        public Option<DataModelValue<float>[]> ReadAll(string key, long? count = null, long? upTo = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            var readOutcome = _hotDb
                .GetAll(key, count, upTo).Values
                .SelectMany(it => it)
                .Select(bytes => TimeSeriesDbExt.FromBytes(bytes, key))
                .OrderBy(mv=>mv.At)
                .ToArray();
            return Option<DataModelValue<float>[]>.Some(readOutcome);
        }

        public Option<Unit> WriteMany(List<IDataModelValue> series, Dictionary<Urn, TimeSpan> seriesDefinitions)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            return SideEffect.TryRun(() =>
                {
                    foreach (var definition in seriesDefinitions)
                    {
                        var _ = SetupTimeSeries(definition.Key.Value, definition.Value);
                    }

                    var sequence = series
                        .Select(it => (Urn: it.Urn.Value, it.At.Ticks, TimeSeriesDbExt.ToBytes(it), it))
                        .ToList();

                    foreach (var (urn, pk, bytes, mv) in sequence)
                    {
                        Log.Verbose("{@DbName}: Upsert {@urn} {@at} {@value}", DbName, urn, mv.At, mv.ModelValue());    
                        _hotDb.Upsert(_hotDb.DefinedStructsByName[urn].First(), pk, bytes);
                    }



                    return default(Unit);
                },
                ex => new Error("", ex.Message)
            ).ValueToOption();
        }

        public void ApplyRetentionPolicy()
        {
            foreach (var (urn,ts) in _retentionPeriod)
            {
                if (ts != TimeSpan.Zero)
                {
                    Log.Verbose("{@DbName}: Apply retention policy {@urn} {@at}", DbName, urn, ts);    
                    DeleteOlderThan(urn, ts);
                }
            }
        }

        public Option<Unit> Write(string key, TimeSpan at, double value)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            var bytes = TimeSeriesDbExt.ToBytes(new DataModelValue<float>(key, (float)value, at));
            //Log.Verbose("{@DbName}: Upsert {@urn} {@at} {@value}", DbName, key, at, value);
            _hotDb.Upsert(_hotDb.DefinedStructsByName[key].First(), at.Ticks, bytes);
            return default(Unit);
        }

        public long Delete(string key, TimeSpan? @from = null, TimeSpan? @to = null)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            if (!_hotDb.IsDefined(key)) return 0;
            @from ??= TimeSpan.MinValue;
            @to ??= TimeSpan.MaxValue;
            var x = @to < @from
                ? 0
                : _hotDb.Delete(key, @from.Value.Ticks, @to.Value.Ticks);
            return x;
        }

        public Result<Unit> SetupTimeSeries(string key, TimeSpan retentionPeriod)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));

            if(!_hotDb.IsDefined(key))
            {
                var def = new StructDef(Guid.NewGuid(), key, BlocksPerSegment,
                    new[] { new FieldDef("at", "long",2,8), new FieldDef("value","float", 1,4) });
                _hotDb.Define(def);
            }

            _retentionPeriod.AddOrUpdate(key, retentionPeriod, (_, _) => retentionPeriod);

            return default(Unit);
        }

        public Option<Unit> DeleteOlderThan(string key, TimeSpan period)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            if (!_hotDb.IsDefined(key)) return Option<Unit>.None();
            //_hotDb.DeleteDeltaLast(key, period.Ticks);
            var bounds = 
                from last in ReadLast(key)
                from first in ReadFirst(key)
                select (last:last.At, first:first.At);
            bounds.Tap(b =>
            {
                var threshold = b.last.Ticks - period.Ticks - 1;
                if (threshold > 0)
                {
                    Log.Verbose("{@DbName}: DeleteOlderThan {@urn} {@at}", DbName, key, threshold);    
                    _hotDb.Delete(key, b.first.Ticks, threshold);
                }
            });    
            return default(Unit);
        }

        public bool IsDefined(string key)
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(_hotDb));
            return _hotDb.IsDefined(key);
        }

        public void Dispose()
        {
            if (IsDisposed) return;
            _hotDb.Dispose();
            IsDisposed = true;
        }

        public bool IsDisposed { get; set; }

        public TimeSpan RetentionTime(string urn)
        {
            return _retentionPeriod.GetValueOrDefault(urn, TimeSpan.Zero);
        }
    }
}