using System;
using System.Data;
using System.Linq;
using static ImpliciX.Data.HotDb.Model.BlockState;
using static ImpliciX.Data.HotDb.Model.SegmentState;

namespace ImpliciX.Data.HotDb.Model;

internal class Planner
{
    private Scribe Scribe { get; }

    public Planner(Scribe scribe)
    {
        Scribe = scribe;
    }
    public Work<WriteContext> DefineStructure(StructDef def)
    {
        return Work<WriteContext>.New.SetupContext((ram, _, ctx) =>
            {
                ctx.ShouldDefineStructure(!ram.ContainsStructure(def));
                return ctx.Set(def);
            }).EnqueueWorkItem((dbFiles, ctx) => Scribe.DefineDataStructure(def, ctx, dbFiles), ctx => ctx.CanDefineStructure)
            .EnqueueRamUpdate((ram, ctx) => ram.AddOrUpdate(ctx.StructDef!), ctx => ctx.HasStructure);
    }

    public Work<WriteContext> Upsert(long pk, byte[] bytes) =>
        Work<WriteContext>.New
            .Ensure(ctx => ctx.StructDef != null, new NoNullAllowedException("InUse structure is null"))
            .SetupContext((ram, dbFiles, ctx) =>
            {
                var structDef = ctx.StructDef!;
                var (seg, isNewSeg) = GetUsableSegmentOrNew(ram,
                    structDef,
                    @default: new Seg(
                        OwnOffset: dbFiles.Segments.EndOfFileOffset,
                        StructDefId: structDef.Id, 
                        UsedSpace: 0,
                        FreeSpace: structDef.DiskCapacityPerSegment,
                        DiskSpace: structDef.DiskCapacityPerSegment,
                        FirstBlockOffset: dbFiles.Blocks.EndOfFileOffset,
                        LastBlockOffset: dbFiles.Blocks.EndOfFileOffset,
                        FirstPk: pk, 
                        LastPk: pk, 
                        State: Reusable)
                );

                var (block, isNewBlock) = Scribe.GetBlockOrNew(pk, seg, 
                    new Block(
                    State: NotDeleted, 
                    OwnOffset: seg.FirstBlockOffset + seg.UsedSpace, 
                    Pk: pk), ram, dbFiles);

                if (isNewBlock)
                    seg = seg.AddBlock(block, bytes.Length);

                return ctx.Set(structDef, seg, isNewSeg, block, isNewBlock);
            })
            .EnqueueWorkItem(Scribe.AllocateSeg, ctx => ctx.CanExecute && ctx.IsNewSegment)
            .EnqueueWorkItem(Scribe.WriteSeg, ctx => ctx.CanExecute)
            .EnqueueWorkItem((io, ctx) => Scribe.WriteBlock(bytes, io, ctx), ctx => ctx.CanExecute)
            .EnqueueRamUpdate((ram, ctx) => ram.AddOrUpdate(ctx.Seg!), ctx => ctx.CanExecute);


    public Work<WriteContext> Delete(string structureName, long pk) =>
        Work<WriteContext>.New
            .SetupContext((ram,disk, ctx) =>
            {
                var _ = GetStructureOrThrow(ram, structureName);

                var segment = ram
                    .SegsOf(structureName)
                    .FirstOrDefault(it => it.ContainsPk(pk));

                if (segment == null) return ctx;
                
                var structDef = ram.GetStructureById(segment.StructDefId);
                var block = Scribe.ReadForwardBlocks(structDef, segment, disk)
                    .Select(it => it.Item2)
                    .FirstOrDefault(b => b.Pk == pk);

                if (block != null)
                {
                    block = block with {State = Deleted};
                    segment = segment.FreeBlocks(structDef, block);
                }

                return ctx.Set(structDef, segment, false, block, false);
            }).EnqueueWorkItem(Scribe.FreeBlock, ctx => ctx.CanExecute)
            .EnqueueWorkItem(Scribe.WriteSeg, ctx => ctx.CanExecute)
            .EnqueueRamUpdate((ram, ctx) => ram.AddOrUpdate(ctx.Seg!), ctx => ctx.CanExecute);

    public Work<BulkDeleteContext> Delete(string structureName, long pkMin, long pkMax) =>
        Work<BulkDeleteContext>.New.SetupContext((ram,disk, ctx) =>
            {
                
                var structure = GetStructureOrThrow(ram, structureName);

                var allStructs = ram.AllStructuresById;
                var segments = ram
                    .SegsOf(structure.Name).Where(it => it.Overlaps(pkMin, pkMax)).ToArray();

                var blocksToDelete = segments
                    .SelectMany(seg => Scribe.ReadForwardBlocks(ram.GetStructureById(seg.StructDefId), seg, disk))
                    .Where(r => r.block.InRange(pkMin, pkMax))
                    .ToArray();

                var newSegments = blocksToDelete
                    .GroupBy(it => it.segment, it => it.block)
                    .Select(g => g.Key.FreeBlocks(allStructs[g.Key.StructDefId], g.ToArray())).ToArray();

                return ctx.SetBlocks(structure, newSegments, blocksToDelete.Select(it => it.block).ToArray());
            })
            .EnqueueWorkItem((dbFiles, ctx) => Scribe.FreeBlocks(ctx, dbFiles), ctx => ctx.CanExecute)
            .EnqueueWorkItem(Scribe.WriteSegs, ctx => ctx.CanExecute)
            .EnqueueRamUpdate((ram, ctx) =>
            {
                foreach (var segment in ctx.Segs!)
                {
                    ram.AddOrUpdate(segment);
                }
            }, ctx => ctx.CanExecute);
    
    public Work<ReadSingleContext> Get(string structureName, long pk) =>
        Work<ReadSingleContext>.New.SetupContext(
            (ram, disk, ctx) =>
            {
                var structure = GetStructureOrThrow(ram, structureName);
                var segment = ram
                    .SegsOf(structure.Name)
                    .FirstOrDefault(it => it.ContainsPk(pk));
                
                if(segment == null)
                    return ctx;
                
                var block = Scribe.ReadForwardBlocks(ram.GetStructureById(segment.StructDefId), segment, disk)
                    .Select(it => it.Item2)
                    .FirstOrDefault(b => b.Pk == pk);

                return ctx.SetBlock(structure, segment, block);
            }
        ).EnqueueWorkItem((disk, ctx) =>
        {
            var block = ctx.Block!;
            var structDef = ctx.StructDef!;
            var (state, payload) = Scribe.ReadBlockPayload(structDef, disk, block);
            return state == NotDeleted ? ctx.SetResults(payload) : ctx;
        }, ctx => ctx.CanExecute);

    public Work<CountContext> Count(StructDef structDef)
    {
        return Work<CountContext>
            .New.SetupContext((ram, _, ctx)=>ctx.Set(structDef, ram.SegsOf(structDef, InUse, NonUsable)))
            .EnqueueWorkItem((_, ctx) =>
            {
                var result = ctx.Segs.Select(seg=>(seg.DiskSpace - seg.FreeSpace)/structDef.BlockDiskSize).Sum();
                return ctx.SetResult(result);
            }, ctx => ctx.CanExecute);
    }
    public Work<ReadManyContext> GetAll(string structureName, long? count = null, long? upTo = null)
    {
        return Work<ReadManyContext>.New
            .SetupContext((ram, _, ctx) => ctx.Set(ram.SegsOf(structureName), ram.AllStructuresById))
            .EnqueueWorkItem((disk, ctx) =>
            {
                var structDefs = ctx.DataStructs;
                var results = ctx.Segs
                    .SelectMany(seg => Scribe.ReadPagesOfSeg(structDefs[seg.StructDefId], seg, disk))
                    .Where(it => it.page.Pk <= (upTo ?? long.MaxValue))
                    .Take((int) (count ?? int.MaxValue))
                    .GroupBy(it => it.structDef, it => it.Item2.Bytes)
                    .ToDictionary(g => g.Key, g => g.ToArray());
                return ctx.SetResults(results);
            }, ctx => ctx.CanExecute);
    }
    public Work<ReadSingleContext> GetFirst(string structureName, long? pkMin)
    {
        Func<Seg,bool> segPredicate = pkMin is null ? s => s.State != Reusable : s => s.ContainsPk(pkMin.Value);
        Func<Block,bool> blockPredicate = pkMin is null ? b => b.State == NotDeleted : b => b.Pk >= pkMin && b.State == NotDeleted;
        return Work<ReadSingleContext>.New
            .SetupContext((ram, disk , ctx) =>
            {
                var structure = GetStructureOrThrow(ram, structureName);
                var segments = ram.SegsOf(structure.Name);
                
                var segment = segments.FirstOrDefault(segPredicate);
                if(segment == null)
                {
                    if(pkMin is null || !segments.Any()) return ctx;

                    var firstSegment = segments.First();
                    if(pkMin.Value < firstSegment.FirstPk)
                        segment = firstSegment;
                    else
                        return ctx;
                }
                var block = Scribe.ReadForwardBlocks(ram.GetStructureById(segment.StructDefId), segment, disk).Select(it => it.Item2).FirstOrDefault(blockPredicate);
                return ctx.SetBlock(structure, segment, block);
            }).EnqueueWorkItem((disk, ctx) =>
            {
                var structDef = ctx.StructDef!;
                var block = ctx.Block!;
                var (state, payload) = Scribe.ReadBlockPayload(structDef, disk, block);
                return state == NotDeleted ? ctx.SetResults(payload) : ctx;
            }, ctx => ctx.CanExecute);
    }

    public Work<ReadSingleContext> GetLast(string structureName, long? pkMax = null)
    {
        Func<Seg,bool> segPredicate = pkMax is null ? s=>s.State != Reusable : s => s.LastPk <= pkMax.Value || s.ContainsPk(pkMax.Value);
        Func<Block,bool> blockPredicate = pkMax is null ? b => b.State == NotDeleted : b => b.Pk <= pkMax && b.State == NotDeleted;
        return Work<ReadSingleContext>.New
            .SetupContext((ram, disk , ctx) =>
            {
                var structure = GetStructureOrThrow(ram, structureName);
                var segment = ram.SegsOf(structure.Name).LastOrDefault(segPredicate);
                if(segment == null)
                    return ctx;
                var block = Scribe.ReadBackwardBlocks(ram.GetStructureById(segment.StructDefId), segment, disk).Select(it => it.Item2).FirstOrDefault(blockPredicate);
                return ctx.SetBlock(structure, segment, block);
            }).EnqueueWorkItem((disk, ctx) =>
            {
                var structDef = ctx.StructDef!;
                var block = ctx.Block!;
                var (state, payload) = Scribe.ReadBlockPayload(structDef,disk, block);
                return state == NotDeleted ? ctx.SetResults(payload) : ctx;
            }, ctx => ctx.CanExecute);
    }

    private static StructDef GetStructureOrThrow(IReadOnlyRam ram, string structureName) =>
        ram.TryGetStructure(structureName, out var structure)
            ? structure
            : throw new Exception("The structure should exist in the ram");


    private static (Seg s, bool isNew) GetUsableSegmentOrNew(IReadOnlyRam ram, StructDef def, Seg @default)
    {
        if (!ram.TryGetSegments(def, out _))
            return (@default, true);

        var inUse = ram.UsableSegsOf(def, InUse).SingleOrDefault();
        if (inUse != null)
        {
            return (inUse, false);
        }
        
        var reUsable = ram.UsableSegsOf(def, Reusable).FirstOrDefault();
        if (reUsable != null)
        {
            return (reUsable, false);
        }
        return (@default, true);
    }


}