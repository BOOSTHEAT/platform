
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.HotDb.Hdw;
using AggregateException = System.AggregateException;
using Exception = System.Exception;

namespace ImpliciX.Data.HotDb.Model;

internal class Work<TCtx> where TCtx:IContext
{
    private readonly List<(System.Func<TCtx, bool> check, Exception ex)> _verifications = new();
    private System.Func<IReadOnlyRam, Disk, TCtx, TCtx> _contextSetup = (_,_, ctx) => ctx;
    private readonly List<DiskWorkItem<TCtx>> _diskWorkItems = new();
    private readonly List<RamWorkItem<TCtx>> _ramWorkItems = new();

    public Work<TCtx> Ensure(System.Func<TCtx, bool> verify, Exception ex)
    {
        _verifications.Add((verify,ex));
        return this;
    }
    
    public Work<TCtx> SetupContext(System.Func<IReadOnlyRam, Disk, TCtx, TCtx> contextSetup)
    {
        _contextSetup = contextSetup;
        return this;
    }
    public Work<TCtx> EnqueueWorkItem(System.Func<Disk, TCtx, TCtx> work, System.Func<TCtx,bool>? pred=null)
    {
       _diskWorkItems.Add(new DiskWorkItem<TCtx>(work, pred ?? (_ => true)));
        return this;
    }
    
    public Work<TCtx> EnqueueRamUpdate(System.Action<IRam, TCtx> work, System.Func<TCtx,bool>? pred=null)
    {
        _ramWorkItems.Add(new RamWorkItem<TCtx>(work, pred ?? (_ => true)));
        return this;
    }
    
    private void Verify(TCtx ctx)
    {
        var failed = _verifications.Where(it => !it.check(ctx)).ToArray();
        if (failed.Any())
        {
            throw new AggregateException(failed.Select(it=>it.ex));
        }
    }

    public TCtx Execute(Disk disk, IRam ram, TCtx ctx) 
    {
        Verify(ctx);
        ctx = _contextSetup(ram,disk,ctx);
        ctx = _diskWorkItems.Aggregate(ctx, (current, workItem) => workItem.Execute(disk, current));
        disk.Flush();
        _ramWorkItems.ForEach(wi=>wi.Execute(ram, ctx));
        return ctx;
    }
    
    public static Work<TCtx> New => new();
}

internal record DiskWorkItem<TCtx>(System.Func<Disk,TCtx,TCtx> Work, System.Func<TCtx, bool> Predicate)
{
   public TCtx Execute(Disk io, TCtx ctx) => 
       Predicate(ctx) ? Work(io,ctx):ctx;
}

internal record RamWorkItem<TCtx>(System.Action<IRam, TCtx> Work, System.Func<TCtx, bool> Predicate)
{
    public TCtx Execute(IRam ram, TCtx ctx)
    {
        if(Predicate(ctx))
            Work(ram, ctx);
        return ctx;
    }
}

internal interface IContext
{
    bool CanExecute { get; }
    public StructDef? StructDef { get; set; }
}


internal class BulkDeleteContext : IContext
{
    public BulkDeleteContext SetBlocks(StructDef structDef, Seg[] newSegments, Block[] blocks)
    {
        StructDef = structDef;
        Segs = newSegments;
        Blocks = blocks;
        return this;
    }

    public Block[]? Blocks { get; set; } 

    public Seg[]? Segs { get; set; }

    public bool CanExecute => StructDef != null && Segs != null && Blocks is {Length: > 0};

    public uint EndOfFileOffset { get; set; }
    public StructDef? StructDef { get; set; }
    
    public int BlocksCount { get; set; }

    public BulkDeleteContext SetResults(int n)
    {
        BlocksCount = n;
        return this;
    }
}

internal class WriteContext : IContext
{
    public StructDef? StructDef { get; set; }
    public Seg? Seg { get; set; }
    public bool IsNewSegment { get; set; }
    public Block? Block { get; set; }
    public bool IsNewBlock { get; set; }

    public WriteContext Set(StructDef structDef, Seg? seg, bool isNewSeg, Block? block, bool isNewBlock)
    {
        StructDef = structDef;
        Seg = seg;
        IsNewSegment = isNewSeg;
        Block = block;
        IsNewBlock = isNewBlock;
        return this;
    }

    public bool CanExecute => StructDef != null && Seg != null && Block != null ;

    public void ShouldDefineStructure(bool flag)
    {
        CanDefineStructure = flag;
    } 
    public bool CanDefineStructure { get; private set; }
    
    public bool HasStructure => StructDef != null;
    
    
    public int BlocksCount { get; set; }

    public WriteContext SetResults(int n)
    {
        BlocksCount = n;
        return this;
    }

    public WriteContext Set(StructDef structDef)
    {
        StructDef = structDef;
        return this;
    }
}

internal class ReadSingleContext: IContext
{
    public StructDef? StructDef { get; set; }
    public Seg? Seg { get; set; }
    internal Block? Block { get; set; }

    public bool CanExecute => StructDef != null && Seg != null && Block != null;
    
    public byte[] Results { get; private set; } = Array.Empty<byte>();
    
    public ReadSingleContext SetResults(byte[] results)
    {
        Results = results;
        return this;
    }

    internal ReadSingleContext SetBlock(StructDef structDef, Seg? seg, Block? block)
    {
        StructDef = structDef;
        Seg = seg;
        Block = block;
        return this;
    }
}

public class ReadManyContext : IContext
{
    
    public StructDef? StructDef { get; set; }

    public bool CanExecute => Segs?.Length > 0;
    
    public Dictionary<StructDef, byte[][]> Results { get; private set; } = new();
    
    public ReadManyContext SetResults(Dictionary<StructDef, byte[][]> results)
    {
        Results = results;
        return this;
    }

    public ReadManyContext Set(Seg[] segs, IReadOnlyDictionary<Guid, StructDef> dataStructs)
    {
        Segs = segs;
        DataStructs = dataStructs;
        return this;
    }

    public IReadOnlyDictionary<Guid, StructDef> DataStructs { get; private set; } = new Dictionary<Guid, StructDef>();


    public Seg[] Segs { get; private set; } = Array.Empty<Seg>();
}

public class CountContext : IContext
{
    public StructDef? StructDef { get; set; }

    public bool CanExecute => Segs?.Length > 0;
    public Seg[] Segs { get; private set; } = Array.Empty<Seg>();
    
    public int Result { get; private set; } = 0;
    
    public CountContext SetResult(int results)
    {
        Result = results;
        return this;
    }
    
    public CountContext Set(StructDef structDef, Seg[] segs)
    {
        StructDef = structDef;
        Segs = segs;
        return this;
    }
}