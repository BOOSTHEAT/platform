using ImpliciX.Data.HotDb;
using ImpliciX.Data.HotDb.Model;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Logger;
using Serilog.Core;
using static System.BitConverter;

namespace Implicix.SharedKernel.Benchmark;

[SimpleJob(RuntimeMoniker.Net70)]
public class HotDbUpsertBench
{
    private HotDb _hotDb;
    private StructDef _structDef;
    private Random _rnd;
    private int _initial_samples;


    [IterationSetup]
    public void Setup()
    {
        var tmpUpsertBench = "/tmp/upsert_bench/";
        if(Directory.Exists(tmpUpsertBench))
            Directory.Delete(tmpUpsertBench, true);

        Log.Logger = new SerilogLogger(Logger.None);

        _rnd = new Random(42);
        _hotDb = HotDb.Create(tmpUpsertBench,"hdb");
        _structDef = new StructDef(Guid.NewGuid(), "bench:upsert", 10_000, new[] { new FieldDef("at", "long", 1,8), new FieldDef("value","float",2,4) });
        _hotDb.Define(_structDef); 
        _initial_samples = 1_000;

        var initialData = GenerateInitialData(1_000_000).ToArray();
        foreach(var (pk, bytes) in initialData)
            _hotDb.Upsert(_structDef, pk, bytes);

        Console.WriteLine("Setup done");
    }

    [Benchmark]
    public void Upsert_The_Update_Case()
    {
        var (pk, bytes) = GenerateSample(0, _initial_samples);
        _hotDb.Upsert(_structDef, pk, bytes);
    }
    
    
    public IEnumerable<(long, byte[])> GenerateInitialData(int nbSamples) =>
        Enumerable.Range(0, nbSamples)
            .Select(i => ((long)i, GetBytes((long)i).Concat(GetBytes(_rnd.NextDouble())).ToArray()));
    
    public (long, byte[]) GenerateSample(long pkMin, long pkMax) =>
        (_rnd.NextInt64(pkMin, pkMax), GetBytes((long)0).Concat(GetBytes(_rnd.NextDouble())).ToArray());
}