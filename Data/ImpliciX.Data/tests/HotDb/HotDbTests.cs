using System;
using System.IO;
using System.Linq;
using System.Text;
using ImpliciX.Data.HotDb.Model;
using NUnit.Framework;
using static System.BitConverter;
using static ImpliciX.Data.HotDb.Model.BlockState;
using static ImpliciX.Data.HotDb.Model.SegmentState;
using static ImpliciX.Data.Tests.HotDb.Helpers;
using Db = ImpliciX.Data.HotDb.HotDb;

namespace ImpliciX.Data.Tests.HotDb;

[NonParallelizable]
[Platform(Include = "Linux")]
public class HotDbTests
{
    [Test]
    public void create_db()
    {
        DbTest(
            arrange: () => Db.Create(FolderPath,DbName),
            assertDisk: _ =>
            {
                Assert.That(Directory.Exists(FolderPath), Is.True);
                Assert.That(File.Exists(Path.Combine(FolderPath, $"{DbName}.structure")), Is.True);
                Assert.That(File.Exists(Path.Combine(FolderPath, $"{DbName}.segments")), Is.True);
                Assert.That(File.Exists(Path.Combine(FolderPath, $"{DbName}.blocks")), Is.True);
            });
    }
    
    [Test]
    public void create_db_in_non_empty_folder()
    {
        Directory.CreateDirectory(FolderPath);
        File.Create(Path.Combine(FolderPath, "foo.bar"));
        Assert.Throws<Exception>(() => Db.Create(FolderPath, DbName));
    }
    
    [Test]
    public void create_one_structure()
    {
        var structDef = new StructDef(Guid.NewGuid(), "a", 10, FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
            },
            assertRam: ram => { Assert.That(ram.ContainsStructure("a"), Is.True); },
            assertDisk: disk =>
            {
                var span = disk.Structure.ReadAllBytes();
                var payloadSize = (int) ToUInt16(span[..2]);
                var expectedPayload = structDef.ToBytes();
                var loadedStruct = StructDef.FromBytes(span[2..]);

                Assert.That(payloadSize, Is.EqualTo(expectedPayload.Length));
                Assert.That(span[2..], Is.EquivalentTo(expectedPayload));
                
                Assert.That(loadedStruct.Id, Is.EqualTo(structDef.Id));
                Assert.That(loadedStruct.BlockDiskSize, Is.EqualTo(13));
                Assert.That(loadedStruct.BlockPayloadSize, Is.EqualTo(12));
                Assert.That(loadedStruct.Name, Is.EqualTo(structDef.Name));
                Assert.That(loadedStruct.Fields, Is.EquivalentTo(structDef.Fields));

            });
    }
    
    [Test]
    public void create_many_structures()
    {
        var structDef1 = new StructDef(Guid.NewGuid(), Name: "a", 10, FieldsDefs);
        var structDef2 = new StructDef(Guid.NewGuid(), Name: "foo",10, new []
        {
            new FieldDef("value1","bool",1,1),
            new FieldDef("value2","short",2,2)
        });
        var structDef3 = new StructDef(Guid.NewGuid(), Name: "foo:bar", 10, new []{
            new FieldDef("foo","long",3,8),
            new FieldDef("bar","float",4, 4)
        });

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef1);
                db.Define(structDef2);
                db.Define(structDef3);
            },
            assertRam: ram =>
            {
                Assert.That(ram.ContainsStructure("a"), Is.True);
                Assert.That(ram.ContainsStructure("foo"), Is.True);
                Assert.That(ram.ContainsStructure("foo:bar"), Is.True);
            },
            assertDisk: disk =>
            {
                var bytes = disk.Structure.ReadAllBytes();
                using var br = new BinaryReader(new MemoryStream(bytes));
                
                var struct1PayloadSize = br.ReadUInt16();
                var struct1Payload = br.ReadBytes(struct1PayloadSize);
                
                var struct2PayloadSize = br.ReadUInt16();
                var struct2Payload = br.ReadBytes(struct2PayloadSize);
                
                var struct3PayloadSize = br.ReadUInt16();
                var struct3Payload = br.ReadBytes(struct3PayloadSize);
                

                Assert.That(struct1Payload, Is.EquivalentTo(structDef1.ToBytes()));
                Assert.That(struct2Payload, Is.EquivalentTo(structDef2.ToBytes()));
                Assert.That(struct3Payload, Is.EquivalentTo(structDef3.ToBytes()));
            });
    }

    [Test]
    public void create_structure_is_idempotent()
    {
        var structDef1 = new StructDef(Id: Guid.NewGuid(), Name: "a", 10, FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef1);
                db.Define(structDef1);
            },
            assertRam: ram =>
            {
                Assert.That(ram.ContainsStructure("a"), Is.True);
            },
            assertDisk: disk =>
            {
                var bytes = disk.Structure.ReadAllBytes();
                using var br = new BinaryReader(new MemoryStream(bytes));
                
                var struct1PayloadSize = br.ReadUInt16();
                var struct1Payload = br.ReadBytes(struct1PayloadSize);

                Assert.That(struct1Payload, Is.EquivalentTo(structDef1.ToBytes()));
            });
    }

   [Test]
    public void create_segment_when_upsert_first_data_block()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10, FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 2f)});
            },
            assertRam: ram =>
            {
                ram.TryGetStructure("a", out var structure);
                Assert.That(structure, Is.Not.Null);
                Assert.That(ram.SegsOf("a").Length, Is.EqualTo(1));
                var seg = ram.SegsOf("a")[0];
                Assert.That(seg.OwnOffset, Is.EqualTo(0));
                Assert.That(seg.UsedSpace, Is.EqualTo(13));
                Assert.That(seg.FreeSpace, Is.EqualTo(117));
                Assert.That(seg.DiskSpace, Is.EqualTo(130));
                Assert.That(seg.StructDefId, Is.EqualTo(structDef.Id));
                Assert.That(seg.FirstBlockOffset, Is.EqualTo(0));
                Assert.That(seg.FirstPk, Is.EqualTo(1L));
                Assert.That(seg.LastPk, Is.EqualTo(1L));
                Assert.That(seg.State, Is.EqualTo(InUse));
            },
            assertDisk: disk =>
            {
                var expectedSize = 2 + sizeof(uint) + 
                                  Encoding.ASCII.GetBytes(Guid.NewGuid().ToString()).Length + 1 +
                                  sizeof(ushort) + sizeof(ushort) +
                                  sizeof(ushort)  + sizeof(uint) + sizeof(uint) + sizeof(long) + sizeof(long) + sizeof(byte);
               Assert.That(disk.Segments.Size, Is.EqualTo(expectedSize));
                
                var segmentBytes = disk.Segments.ReadAllBytes();
                using var br = new BinaryReader(new MemoryStream(segmentBytes));
                var payloadSize = br.ReadUInt16();
                var payloadBytes = br.ReadBytes(payloadSize);

                var seg = Seg.FromBytes(payloadBytes);
                
                Assert.That(seg.OwnOffset, Is.EqualTo(0));
                Assert.That(seg.UsedSpace, Is.EqualTo(13));
                Assert.That(seg.FreeSpace, Is.EqualTo(117));
                Assert.That(seg.DiskSpace, Is.EqualTo(130));
                Assert.That(seg.StructDefId, Is.EqualTo(structDef.Id));
                Assert.That(seg.FirstBlockOffset, Is.EqualTo(0));
                Assert.That(seg.LastBlockOffset, Is.EqualTo(0));
                Assert.That(seg.FirstPk, Is.EqualTo(1L));
                Assert.That(seg.LastPk, Is.EqualTo(1L));
                Assert.That(seg.State, Is.EqualTo(InUse));

                var blocks = ExtractBlocksData(disk.Blocks.ReadAllBytes(), structDef);
                Assert.That(blocks, Is.EquivalentTo(new []{(Used: NotDeleted, 1L, 2f)}));
            });
    }

    [Test]
    public void create_many_data_blocks()
    {
                
                var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

                DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 2f), (2L, 3f), (3L, 4f)});
            },
            assertRam: ram =>
            {
                ram.TryGetStructure("a", out var structure);
                Assert.That(structure, Is.Not.Null);
                Assert.That(ram.SegsOf("a").Length, Is.EqualTo(1));
                var seg = ram.SegsOf("a")[0];
                Assert.That(seg.OwnOffset, Is.EqualTo(0));
                Assert.That(seg.UsedSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(seg.DiskSpace, Is.EqualTo(structDef.DiskCapacityPerSegment));
                Assert.That(seg.StructDefId, Is.EqualTo(structDef.Id));
                Assert.That(seg.FirstBlockOffset, Is.EqualTo(0));
                Assert.That(seg.FirstPk, Is.EqualTo(1L));
                Assert.That(seg.LastPk, Is.EqualTo(3L));
                Assert.That(seg.State, Is.EqualTo(InUse));
            },
            assertDisk: disk =>
            {
                var blockData = ExtractBlocksData(disk.Blocks.ReadAllBytes(), structDef);
                Assert.That(blockData, Is.EquivalentTo(new[] { (NotDeleted, 1L, 2f), (NotDeleted, 2L, 3f), (NotDeleted, 3L, 4f) }));
            });
    }
    
    [Test] 
    public void load_a_database()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);
        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 142f), (2L, 243f), (3L, 344f)});
                UpsertMany(db, "a", new[] {(3L, 342f), (4L, 443f), (5L, 544f)});
                UpsertMany(db, "a", new[] {(6L, 642f), (7L, 743f)});
                db.Delete("a",1L);
                db.Delete("a",5L, 6L);
                return db;
            },
            db =>
            {
                db.Dispose();
                return Db.Load(FolderPath, DbName);
            },
            assertRam: ram =>
            {
                Assert.That(ram.ContainsStructure("a"), Is.True);
                var loadedStructDef = ram.GetStructureById(structDef.Id);
                Assert.That(loadedStructDef, Is.Not.Null);
                Assert.That(loadedStructDef.Id, Is.EqualTo(structDef.Id));
                Assert.That(loadedStructDef.Name, Is.EqualTo(structDef.Name));
                Assert.That(loadedStructDef.BlockDiskSize, Is.EqualTo(structDef.BlockDiskSize));
                Assert.That(loadedStructDef.BlockPayloadSize, Is.EqualTo(structDef.BlockPayloadSize));
                Assert.That(loadedStructDef.Fields, Is.EquivalentTo(structDef.Fields));
                
                Assert.That(ram.SegsOf("a").Length, Is.EqualTo(3));
                var segs = ram.SegsOf("a");
                
                Assert.That(segs[0].OwnOffset, Is.EqualTo(0 * Seg.SizeOnDisk));
                Assert.That(segs[0].UsedSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segs[0].FreeSpace, Is.EqualTo(1 * structDef.BlockDiskSize));
                Assert.That(segs[0].DiskSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segs[0].StructDefId, Is.EqualTo(structDef.Id));
                Assert.That(segs[0].FirstBlockOffset, Is.EqualTo(0));
                Assert.That(segs[0].LastBlockOffset, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segs[0].FirstPk, Is.EqualTo(1L));
                Assert.That(segs[0].LastPk, Is.EqualTo(3L));
                Assert.That(segs[0].State, Is.EqualTo(NonUsable));
                
                Assert.That(segs[1].OwnOffset, Is.EqualTo(1 * Seg.SizeOnDisk));
                Assert.That(segs[1].UsedSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segs[1].FreeSpace, Is.EqualTo(1 * structDef.BlockDiskSize));
                Assert.That(segs[1].DiskSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segs[1].StructDefId, Is.EqualTo(structDef.Id));
                Assert.That(segs[1].FirstBlockOffset, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segs[1].LastBlockOffset, Is.EqualTo(5 * structDef.BlockDiskSize));
                Assert.That(segs[1].FirstPk, Is.EqualTo(3L));
                Assert.That(segs[1].LastPk, Is.EqualTo(5L));
                Assert.That(segs[1].State, Is.EqualTo(NonUsable));
                
                Assert.That(segs[2].OwnOffset, Is.EqualTo(2 * Seg.SizeOnDisk));
                Assert.That(segs[2].UsedSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segs[2].FreeSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segs[2].DiskSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segs[2].StructDefId, Is.EqualTo(structDef.Id));
                Assert.That(segs[2].FirstBlockOffset, Is.EqualTo(2 * 3 * structDef.BlockDiskSize));
                Assert.That(segs[2].LastBlockOffset, Is.EqualTo(7 * structDef.BlockDiskSize));
                Assert.That(segs[2].FirstPk, Is.EqualTo(6L));
                Assert.That(segs[2].LastPk, Is.EqualTo(7L));
                Assert.That(segs[2].State, Is.EqualTo(InUse));
            },
            assertDisk: disk =>
            {
                var blockBytes = disk.Blocks.ReadAllBytes();
                var blockData = ExtractBlocksData(blockBytes, structDef);
                Assert.That(blockData, Is.EquivalentTo(new[]
                {
                    (Deleted, 1L, 142f), (NotDeleted, 2L, 243f), (NotDeleted, 3L, 344f), 
                    (NotDeleted, 3L, 342f), (NotDeleted, 4L, 443f), (Deleted, 5L, 544f),
                    (Deleted, 6L, 642f), (NotDeleted, 7L, 743f)
                }));
            });
    }
    
     [Test] 
    public void safe_load_a_database_with_corrupted_files()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);
        DbTest(
            () =>
            {
                var corruptedBytes = new byte[]{1,2,3,4,5,6,7,8,9,10};
                Directory.CreateDirectory(FolderPath);
                File.WriteAllBytes(Path.Combine(FolderPath,$"{DbName}.structure"), corruptedBytes);
                File.WriteAllBytes(Path.Combine(FolderPath,$"{DbName}.segments"), corruptedBytes);
                File.WriteAllBytes(Path.Combine(FolderPath, $"{DbName}.blocks"), corruptedBytes);
                return default;
            },
            _ => default(Db),
            assertOutcome: _ =>
            {
                var db =  Db.Load(FolderPath, DbName, true);

                //corrupted files are moved to quarantine
                var quarantineDirs = Directory.EnumerateDirectories(Path.Combine(FolderPath, "quarantine")).ToArray();
                Assert.That(quarantineDirs.Length, Is.EqualTo(1));
                var quarantineFiles = Directory.EnumerateFiles(quarantineDirs[0]).ToArray();
                Assert.That(quarantineFiles.Length, Is.EqualTo(3));
                Assert.That(quarantineFiles.Select(f=>new FileInfo(f).Name).ToArray(), Is.EquivalentTo(new []{$"{DbName}.structure", $"{DbName}.segments", $"{DbName}.blocks"}));
                
                //and new empty db files are created
                var newFiles = Directory.EnumerateFiles(FolderPath).ToArray();
                Assert.That(newFiles.Length, Is.EqualTo(3));
                Assert.That(newFiles.Select(f=> new FileInfo(f).Name).ToArray(), Is.EquivalentTo(new []{$"{DbName}.structure", $"{DbName}.segments", $"{DbName}.blocks"}));
                Assert.That(newFiles.All(f=>new FileInfo(f).Length ==0), Is.True);
                
                //and the db is usable
                Assert.DoesNotThrow(() =>
                {
                    db.Define(structDef);
                    UpsertMany(db, "a", new[] {(1L, 142f), (2L, 243f), (3L, 344f)});
                });
            });
    }
    
         [Test] 
    public void safe_load_a_database_with_missing_files()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);
        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Dispose();
                File.Delete(Path.Combine(FolderPath,$"{DbName}.structure"));
                return default;
            },
            _ => default(Db),
            assertOutcome: _ =>
            {
                var db =  Db.Load(FolderPath, DbName,true);

                //corrupted files are moved to quarantine
                var quarantineDirs = Directory.EnumerateDirectories(Path.Combine(FolderPath, "quarantine")).ToArray();
                Assert.That(quarantineDirs.Length, Is.EqualTo(1));
                var quarantineFiles = Directory.EnumerateFiles(quarantineDirs[0]).ToArray();
                Assert.That(quarantineFiles.Length, Is.EqualTo(2));
                Assert.That(quarantineFiles.Select(f=>new FileInfo(f).Name).ToArray(), Is.EquivalentTo(new []{$"{DbName}.segments", $"{DbName}.blocks"}));
                
                //and new empty db files are created
                var newFiles = Directory.EnumerateFiles(FolderPath).ToArray();
                Assert.That(newFiles.Length, Is.EqualTo(3));
                Assert.That(newFiles.Select(f=> new FileInfo(f).Name).ToArray(), Is.EquivalentTo(new []{$"{DbName}.structure", $"{DbName}.segments", $"{DbName}.blocks"}));
                Assert.That(newFiles.All(f=>new FileInfo(f).Length ==0), Is.True);
                
                //and the db is usable
                Assert.DoesNotThrow(() =>
                {
                    db.Define(structDef);
                    UpsertMany(db, "a", new[] {(1L, 142f), (2L, 243f), (3L, 344f)});
                });
            });
    }
    
    [Test]
    public void fail_fast_should_throw_a_database_with_corrupted_files()
    {
        DbTest(
            () =>
            {
                var corruptedBytes = new byte[]{1,2,3,4,5,6,7,8,9,10};
                Directory.CreateDirectory(FolderPath);
                File.WriteAllBytes(Path.Combine(FolderPath, $"{DbName}.structure"), corruptedBytes);
                File.WriteAllBytes(Path.Combine(FolderPath, $"{DbName}.segments"), corruptedBytes);
                File.WriteAllBytes(Path.Combine(FolderPath, $"{DbName}.blocks"), corruptedBytes);
                return default;
            },
            _ => default(Db),
            assertOutcome: _ =>
            {
                Assert.Throws(Is.AssignableTo(typeof(Exception)), () => Db.Load(FolderPath, DbName, safeLoad:false));
            });
    }

    [Test]
    public void update_existing_data_block()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 2f), (2L, 3f), (1L, 283f)});
            },
            assertRam: _ =>{},
            assertDisk: disk =>
            {
                var blockData = ExtractBlocksData(disk.Blocks.ReadAllBytes(), structDef);
                Assert.That(blockData, Is.EquivalentTo(new[] { (Used: NotDeleted, 1L, 283f), (Used: NotDeleted, 2L, 3f) }));
            });

    }
    

    [Test]
    public void read_all_data_blocks()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 2f), (2L, 3f), (3L, 283f)});
                return db.GetAll("a");
            },
            assertOutcome: o =>
            { 
                var blocsData = o.Values.SelectMany(b => b).Select(DecodeBlockPayload).ToArray();
                Assert.That(blocsData, Is.EqualTo(new []
                {
                    (1L, 2f),
                    (2L, 3f),
                    (3L, 283f)
                }));
            });
    }

    [Test]
    public void block_by_pk()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 2f), (2L, 3f), (1L, 283f)});
                return db.Get("a", 1L);
            },
            assertOutcome: o =>
            {
                var blockData = DecodeBlockPayload(o);
                Assert.That(blockData, Is.EqualTo((1L, 283f)));
            });
    }

    [Test]
    public void simple_delete_test()
    {
        var structDefA = new StructDef(Id: Guid.NewGuid(), Name: "a", 10, FieldsDefs);
        DbTest(
            arrange: () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDefA);
                UpsertMany(db, "a", new[] {(1L, 12f), (2L, 13f)});
                return db;
            },
            act: db =>
            {
                db.Delete("a", 1L);

            },
            assertRam: ram =>
            {
                var segsA = ram.SegsOf("a");
                
                Assert.That(segsA.Length, Is.EqualTo(1));
                
                Assert.That(segsA[0].UsedSpace, Is.EqualTo(2 * structDefA.BlockDiskSize));
                Assert.That(segsA[0].FreeSpace, Is.EqualTo(9 * structDefA.BlockDiskSize));
                Assert.That(segsA[0].State, Is.EqualTo(InUse));
                
            },
            assertDisk: disk =>
            {
                var blocksData = disk.Blocks.ReadAllBytes();

                var blocksSegsA = ExtractBlocksData(blocksData, structDefA);
                
                Assert.That(blocksSegsA, Is.EquivalentTo(new[] { (Deleted, 1L, 12f), (NotDeleted, 2L, 13f) }));
            });
    }
    
    [Test]
    public void delete_twice_same_block_test()
    {
        var structDefA = new StructDef(Id: Guid.NewGuid(), Name: "a", 10, FieldsDefs);
        DbTest(
            arrange: () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDefA);
                UpsertMany(db, "a", new[] {(1L, 12f), (2L, 13f)});
                return db;
            },
            act: db =>
            {
                db.Delete("a", 1L);
                db.Delete("a", 1L);

            },
            assertRam: ram =>
            {
                var segsA = ram.SegsOf("a");
                
                Assert.That(segsA.Length, Is.EqualTo(1));
                
                Assert.That(segsA[0].UsedSpace, Is.EqualTo(2 * structDefA.BlockDiskSize));
                Assert.That(segsA[0].FreeSpace, Is.EqualTo(9 * structDefA.BlockDiskSize));
                Assert.That(segsA[0].State, Is.EqualTo(InUse));
                
            },
            assertDisk: disk =>
            {
                var blocksData = disk.Blocks.ReadAllBytes();

                var blocksSegsA = ExtractBlocksData(blocksData, structDefA);
                
                Assert.That(blocksSegsA, Is.EquivalentTo(new[] { (Deleted, 1L, 12f), (NotDeleted, 2L, 13f) }));
            });
        
    }
    
    [Test]
    public void delete_many_in_different_structures_test()
    {
        var structDefA = new StructDef(Id: Guid.NewGuid(), Name: "a", 10, FieldsDefs);
        var structDefB = new StructDef(Id: Guid.NewGuid(), Name: "b", 10, FieldsDefs);

        DbTest(
            arrange: () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDefA);
                db.Define(structDefB);
                UpsertMany(db, "a", new[] {(1L, 12f), (2L, 13f), (3L, 1283f)});
                UpsertMany(db, "b", new[] {(1L, 42f), (2L, 43f), (3L, 4283f)});
                return db;
            },
            act: db =>
            {
                db.Delete("a", 1L);
                db.Delete("a", 2L);
                db.Delete("a", 3L);
                db.Delete("b", 3L);

            },
            assertRam: ram =>
            {
                var segsA = ram.SegsOf("a");
                var segsB = ram.SegsOf("b");
                
                Assert.That(segsA.Length, Is.EqualTo(1));
                Assert.That(segsB.Length, Is.EqualTo(1));
                
                Assert.That(segsA[0].UsedSpace, Is.EqualTo(0));
                Assert.That(segsA[0].FreeSpace, Is.EqualTo(10 * structDefA.BlockDiskSize));
                Assert.That(segsA[0].State, Is.EqualTo(Reusable));
                
                Assert.That(segsB[0].UsedSpace, Is.EqualTo(3 * structDefB.BlockDiskSize));
                Assert.That(segsB[0].FreeSpace, Is.EqualTo(8 * structDefB.BlockDiskSize));
                Assert.That(segsB[0].State, Is.EqualTo(InUse));
            },
            assertDisk: disk =>
            {
                var blocksData = disk.Blocks.ReadAllBytes();

                var blocksSegsA = ExtractBlocksData(blocksData[..(10 * structDefA.BlockDiskSize)], structDefA);
                var blocksSegsB = ExtractBlocksData(blocksData[(10 * structDefA.BlockDiskSize)..], structDefB);
                
                Assert.That(blocksSegsA, Is.EquivalentTo(new[] { (Deleted, 1L, 12f), (Deleted, 2L, 13f), (Deleted, 3L, 1283f) }));
                Assert.That(blocksSegsB, Is.EquivalentTo(new[] { (NotDeleted, 1L, 42f), (NotDeleted, 2L, 43f), (Deleted, 3L, 4283f) }));
            });
    }
    
    [Test]
    public void consider_segment_non_usable_when_it_has_not_enough_space_for_new_block()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 2, FieldsDefs);

        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 42f), (2L, 43f), (3L, 44f)});
                return db;
            },
            assertRam: ram =>
            {
                var segs = ram.SegsOf("a");
                Assert.That(segs.Length, Is.EqualTo(2));
                Assert.That(segs[0].UsedSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segs[0].State, Is.EqualTo(NonUsable));
                Assert.That(segs[1].UsedSpace, Is.EqualTo(1 * structDef.BlockDiskSize));
                Assert.That(segs[1].State, Is.EqualTo(InUse));
            });
    }
    
    [Test]
    public void when_all_blocks_are_deleted_the_segment_is_reusable()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);

        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 42f), (2L, 43f), (3L, 44f)});
                return db;
            },
            act: db =>
            {
                db.Delete("a", 1L);
                db.Delete("a", 2L);
                db.Delete("a", 3L);
                return db;
            },
            assertRam: o =>
            {
                var segments = o.SegsOf("a");
                Assert.That(segments.Length, Is.EqualTo(1));
                Assert.That(segments[0].UsedSpace, Is.EqualTo(0));
                Assert.That(segments[0].FreeSpace, Is.EqualTo(structDef.DiskCapacityPerSegment));
                Assert.That(segments[0].State, Is.EqualTo(Reusable));
                Assert.That(segments[0].FirstBlockOffset, Is.EqualTo(0 * structDef.BlockDiskSize));
                Assert.That(segments[0].LastBlockOffset, Is.EqualTo(0 * structDef.BlockDiskSize));
                Assert.That(segments[0].FirstPk, Is.EqualTo(0));
                Assert.That(segments[0].LastPk, Is.EqualTo(0));
                
                
            },
            assertDisk: disk =>
            {
                var blocksData = disk.Blocks.ReadAllBytes();
                var blocks = ExtractBlocksData(blocksData, structDef);
                Assert.That(blocks, Is.EquivalentTo(new[] { (Free: Deleted, 1L, 42f), (Free: Deleted, 2L, 43f), (Free: Deleted, 3L, 44.0f)}));
            });
    }

    [Test]
    public void when_segment_is_reusable_after_each_upsert_delete_sequence()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a",3, FieldsDefs);

        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                return db;
            },
            act: db =>
            {
                db.Upsert(structDef, 1L, EncodeBlockPayload(1L, 42f));
                db.Delete("a", 1L);
                db.Upsert(structDef, 2L, EncodeBlockPayload(2L, 43f));
                db.Delete("a", 2L);
                db.Upsert(structDef, 3L, EncodeBlockPayload(3L, 44f));
                db.Delete("a", 3L);
                return db;
            },
            assertRam: o =>
            {
                var segments = o.SegsOf("a");
                Assert.That(segments.Length, Is.EqualTo(1));
                Assert.That(segments[0].UsedSpace, Is.EqualTo(0));
                Assert.That(segments[0].FreeSpace, Is.EqualTo(structDef.DiskCapacityPerSegment));
                Assert.That(segments[0].State, Is.EqualTo(Reusable));
                Assert.That(segments[0].FirstBlockOffset, Is.EqualTo(0 * structDef.BlockDiskSize));
                Assert.That(segments[0].LastBlockOffset, Is.EqualTo(0 * structDef.BlockDiskSize));
                Assert.That(segments[0].FirstPk, Is.EqualTo(0));
                Assert.That(segments[0].LastPk, Is.EqualTo(0));
                
                
            },
            assertDisk: disk =>
            {
                var blocksData = disk.Blocks.ReadAllBytes();
                var blocks = ExtractBlocksData(blocksData, structDef);
                Assert.That(blocks, Is.EquivalentTo(new[] {(Free: Deleted, 3L, 44.0f)}));
            });
    } 
    
    [Test]
    public void an_reusable_segment_is_reused()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);

        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 42f), (2L, 43f), (3L, 44f)});
                return db;
            },
            act: db =>
            {
                db.Delete("a", 1L);
                db.Delete("a", 2L);
                db.Delete("a", 3L);
                UpsertMany(db, "a", new[] {(4L, 442f), (5L, 543f)});
                return db;
            },
            assertRam: o =>
            {
                var segments = o.SegsOf("a");
                Assert.That(segments.Length, Is.EqualTo(1));
                Assert.That(segments[0].UsedSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segments[0].State, Is.EqualTo(InUse));
                Assert.That(segments[0].FirstPk, Is.EqualTo(4L));
                Assert.That(segments[0].LastPk, Is.EqualTo(5L));
                Assert.That(segments[0].FirstBlockOffset, Is.EqualTo(0 * structDef.BlockDiskSize));
                Assert.That(segments[0].LastBlockOffset, Is.EqualTo(1 * structDef.BlockDiskSize));
                
            },
            assertDisk: disk =>
            {
                var blocksData = disk.Blocks.ReadAllBytes();
                var blocks = ExtractBlocksData(blocksData, structDef);
                Assert.That(blocks, Is.EquivalentTo(new[] { (NotDeleted, 4L, 442f), (NotDeleted, 5L, 543f), (Deleted, 3, 44.0f)}));
            });
    }
    
    [Test]
    public void keep_segment_in_state_non_usable_until_all_the_blocks_are_deleted()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);

        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[]
                {
                    (1L, 42f), //seg 0
                    (2L, 43f),
                    (3L, 44f),
                });
                return db;
            },
            act: db =>
            {
                var states = new SegmentState[3];
                db.Delete("a", 3L);
                states[0] = db.Ram.SegsOf("a")[0].State;
                db.Delete("a", 2L);
                states[1] = db.Ram.SegsOf("a")[0].State;
                db.Delete("a", 1L);
                states[2] = db.Ram.SegsOf("a")[0].State;
                return states;
            },
            assertOutcome: states =>
            {
                Assert.That(states[0], Is.EqualTo(NonUsable));
                Assert.That(states[1], Is.EqualTo(NonUsable));
                Assert.That(states[2], Is.EqualTo(Reusable));
            });
    }

    
    [Test]
    public void create_new_segment_when_some_blocks_have_been_deleted_from_a_non_usable_segment_which_is_not_reusable_yet()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);

        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 42f), (2L, 43f), (3L, 44f)});
                return db;
            },
            act: db =>
            {
                db.Delete("a", 1L);
                UpsertMany(db, "a", new[] {(4L, 442f)});
                return db;
            }, assertRam: o =>
            {
                var segments = o.SegsOf("a");
                Assert.That(segments.Length, Is.EqualTo(2));
                Assert.That(segments[0].UsedSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segments[0].FreeSpace, Is.EqualTo(1 * structDef.BlockDiskSize));
                Assert.That(segments[0].FirstPk, Is.EqualTo(1L));
                Assert.That(segments[0].LastPk, Is.EqualTo(3L));

                Assert.That(segments[1].UsedSpace, Is.EqualTo(1 * structDef.BlockDiskSize));
                Assert.That(segments[1].FreeSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segments[1].FirstPk, Is.EqualTo(4L));
                Assert.That(segments[1].LastPk, Is.EqualTo(4L));
            });
    }

    [Test]
    public void delete_all_blocks_having_the_pk_in_given_range()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);
        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[]
                {
                    (1L, 42f), (2L, 43f), (3L, 44f), //seg 0
                    (4L, 45f), (5L, 46f), (6L, 47f), //seg 1
                    (7L, 48f), (8L, 49f)             //seg 2
                });
                return db;
            },
            act: db=>
            {
                db.Delete("a", 3L, 7L);
                return db;
            },
            assertRam: ram =>
            {
                var segments = ram.SegsOf("a").OrderBy(it=>it.OwnOffset).ToArray();
                Assert.That(segments.Length, Is.EqualTo(3));
                Assert.That(segments[0].UsedSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segments[0].FreeSpace, Is.EqualTo(1 * structDef.BlockDiskSize));
                Assert.That(segments[0].State, Is.EqualTo(NonUsable));
                
                Assert.That(segments[1].UsedSpace, Is.EqualTo(0));
                Assert.That(segments[1].FreeSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segments[1].State, Is.EqualTo(Reusable));
                
                Assert.That(segments[2].UsedSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segments[2].FreeSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segments[2].State, Is.EqualTo(InUse));
            },
            assertDisk: disk =>
            {
                var blocksBytes = disk.Blocks.ReadAllBytes();
                var blocks = ExtractBlocksData(blocksBytes, structDef);
                Assert.That(blocks, Is.EquivalentTo(new[]
                {
                    (NotDeleted, 1L, 42f), (NotDeleted, 2L, 43f), (Deleted, 3L, 44f), //seg 0
                    (Deleted, 4L, 45f), (Deleted, 5L, 46f), (Deleted, 6L, 47f), //seg 1
                    (Deleted, 7L, 48f), (NotDeleted, 8L, 49f)                   //seg 2
                }));
            }
        );
    }

    [Test]
    public void delete_twice_the_same_blocks()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);
        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[]
                {
                    (1L, 42f), (2L, 43f), (3L, 44f), //seg 0
                    (4L, 45f), (5L, 46f), (6L, 47f), //seg 1
                    (7L, 48f), (8L, 49f)             //seg 2
                });
                return db;
            },
            act: db=>
            {
                db.Delete("a", 3L, 7L);
                db.Delete("a", 3L, 7L);
                return db;
            },
            assertRam: ram =>
            {
                var segments = ram.SegsOf("a").OrderBy(it=>it.OwnOffset).ToArray();;
                Assert.That(segments.Length, Is.EqualTo(3));
                Assert.That(segments[0].UsedSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segments[0].FreeSpace, Is.EqualTo(1 * structDef.BlockDiskSize));
                Assert.That(segments[0].State, Is.EqualTo(NonUsable));
                
                Assert.That(segments[1].UsedSpace, Is.EqualTo(0 * structDef.BlockDiskSize));
                Assert.That(segments[1].FreeSpace, Is.EqualTo(3 * structDef.BlockDiskSize));
                Assert.That(segments[1].State, Is.EqualTo(Reusable));
                
                Assert.That(segments[2].UsedSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segments[2].FreeSpace, Is.EqualTo(2 * structDef.BlockDiskSize));
                Assert.That(segments[2].State, Is.EqualTo(InUse));
            },
            assertDisk: disk =>
            {
                var blocksBytes = disk.Blocks.ReadAllBytes();
                var blocks = ExtractBlocksData(blocksBytes, structDef);
                Assert.That(blocks, Is.EquivalentTo(new[]
                {
                    (NotDeleted, 1L, 42f), (NotDeleted, 2L, 43f), (Deleted, 3L, 44f), //seg 0
                    (Deleted, 4L, 45f), (Deleted, 5L, 46f), (Deleted, 6L, 47f), //seg 1
                    (Deleted, 7L, 48f), (NotDeleted, 8L, 49f)                   //seg 2
                }));
            }
        );
    }

    
    [Test]
    public void continue_to_write_at_the_end_of_the_segment_after_delete()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                
                db.Define(structDef);
                UpsertMany(db,"a", new []
                {
                    (10L, 2f), (20L, 3f), (30L, 4f), (40L, 5f), (50L, 6f), (60L, 7f),
                    (70L, 8f)
                });
                db.Delete("a", 10L, 29L);
                UpsertMany(db,"a", new []
                {
                    (80L, 9f), 
                });
            },
            assertRam: ram =>
            { 
                var segments = ram.SegsOf("a");
                Assert.That(segments.Length, Is.EqualTo(1));
                Assert.That(segments[0].UsedSpace, Is.EqualTo(8 * structDef.BlockDiskSize));
                Assert.That(segments[0].FreeSpace, Is.EqualTo(4 * structDef.BlockDiskSize));
            },
            assertDisk: disk =>
            {
                var blocksBytes = disk.Blocks.ReadAllBytes();
                var blocks = ExtractBlocksData(blocksBytes, structDef);
                Assert.That(blocks, Is.EquivalentTo(new[]
                {
                    (Deleted, 10L, 2f), (Deleted, 20L, 3f), (NotDeleted, 30L, 4f), (NotDeleted, 40L, 5f), (NotDeleted, 50L, 6f), (NotDeleted, 60L, 7f),
                    (NotDeleted, 70L, 8f), (NotDeleted, 80L, 9f)
                }));
            }
        );
        
    }

    [Test]
    public void deleted_blocks_cant_be_retrieved_from_db()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3, FieldsDefs);
        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[]
                {
                    (1L, 42f), (2L, 43f), (3L, 44f),//seg 0
                    (4L, 45f), (5L, 46f)            //seg 1
                    
                });
                return db;
            },
            act: db=>
            {
                db.Delete("a", 1L);
                db.Delete("a", 2L);
                return db.GetAll("a");
            },
            assertOutcome: o =>
            {
                var blocks = o.Values.SelectMany(it => it).Select(DecodeBlockPayload).ToArray();
                Assert.That(blocks, Is.EquivalentTo(new[]
                {
                    (3L, 44f),              //seg 0
                    (4L, 45f), (5L, 46f)    //seg 1
                }));
            }
        );
    }

    [Test]
    public void upsert_on_deleted_pk()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 2, FieldsDefs);
        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                db.Upsert(structDef, 1L, GetBytes(1L).Concat(GetBytes(4f)).ToArray() );
                db.Delete("a", 1L);
                db.Upsert(structDef, 1L, GetBytes(1L).Concat(GetBytes(42f)).ToArray());
                return db;
            },
            assertRam: ram =>
            {
                var segments = ram.SegsOf("a");
                Assert.That(segments[0].UsedSpace, Is.EqualTo(1 * structDef.BlockDiskSize));
                Assert.That(segments[0].State, Is.EqualTo(InUse));
                Assert.That(segments[0].FirstPk, Is.EqualTo(1L));
                Assert.That(segments[0].LastPk, Is.EqualTo(1L));
            },
            assertDisk: disk =>
            {
                var blocksData = disk.Blocks.ReadAllBytes();
                var blocks = ExtractBlocksData(blocksData, structDef);
                Assert.That(blocks, Is.EquivalentTo(new[] {(Used: NotDeleted, 1L, 42f)}));
            }
        );
    }

    [Test]
    public void keep_track_of_the_last_pk_inserted()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 2, FieldsDefs);
        DbTest(
            () =>
            {
                var db = Db.Create(FolderPath, DbName);
                db.Define(structDef);
                UpsertMany(db, "a", new[] {(1L, 2f), (2L, 3f), (3L, 283f)});
                UpsertMany(db, "a", new[] {(4L, 12f), (5L, 3f), (6L, 283f)});
                UpsertMany(db, "a", new []{(1L, 42f),(3L, 43f)});
                return db;
            },
            assertRam: ram =>
            {
                Assert.That(ram.LastPkOf(structDef.Id), Is.EqualTo(6L));
            }
        );
        
        
    }

    [Test]
    public void get_last_in_empty_db()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a",10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                return db.GetLast("a");
            },
            assertOutcome: _ =>
            { 
                Assert.Pass();
            });
    }
    
    [Test]
    public void get_first_in_empty_db()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                return db.GetFirst("a");
            },
            assertOutcome: _ =>
            { 
                Assert.Pass();
            });
    }
    
    [Test]
    public void get_in_empty_db()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                return db.Get("a",1);
            },
            assertOutcome: _ =>
            { 
                Assert.Pass();
            });
    }
    
    [Test]
    public void get_when_pk_not_found()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db,"a", new []{(1L, 2f)});
                return db.Get("a",2);
            },
            assertOutcome: o =>
            { 
                Assert.That(o, Is.EquivalentTo(Array.Empty<byte>()));
            });
    }

    [Test]
    public void get_last()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db,"a", new []{(1L, 2f), (2L, 3f), (3L, 4f)});
                return db.GetLast("a");
            },
            assertOutcome: o =>
            { 
                var (pk, value) = DecodeBlockPayload(o);
                Assert.That(pk, Is.EqualTo(3L));
                Assert.That(value, Is.EqualTo(4f));
            });
    }
    
    [Test]
    public void get_first()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db,"a", new []{(1L, 2f), (2L, 3f), (3L, 4f)});
                return db.GetFirst("a");
            },
            assertOutcome: o =>
            { 
                var (pk, value) = DecodeBlockPayload(o);
                Assert.That(pk, Is.EqualTo(1L));
                Assert.That(value, Is.EqualTo(2f));
            });
    }

    [TestCaseSource(nameof(GetLastWithUpperLimitCases))]
    public void get_last_with_upper_limit((long, float)[] data, long limit, (long, float)? expected)
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db,"a", data);
                return db.GetLast("a", limit);
            },
            assertOutcome: o =>
            { 
                if (expected != null)
                {
                    Assert.That(DecodeBlockPayload(o), Is.EqualTo(expected));
                }
                else
                {
                    Assert.That(o, Is.EquivalentTo(Array.Empty<byte>()));
                }
            });
    }
    
    [TestCaseSource(nameof(GetFirstWithLowerLimitCases))]
    public void get_first_with_lower_limit((long, float)[] data, long limit, (long, float)? expected)
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a",  10,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db,"a", data);
                return db.GetFirst("a", limit);
            },
            assertOutcome: o =>
            {
                if (expected != null)
                {
                    Assert.That(DecodeBlockPayload(o), Is.EqualTo(expected));
                }
                else
                {
                    Assert.That(o, Is.EquivalentTo(Array.Empty<byte>()));
                }
                
            });
    }
   
    [Test]
    public void when_last_is_deleted_get_last_should_return_the_penultimate()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db,"a", new []
                {
                    (1L, 2f), (2L, 3f), (3L, 4f), //seg 0
                    (4L, 5f), (5L, 6f), (6L, 7f),  //seg 1
                    (7L, 8f), (8L, 9f), //seg 2
                });
                db.Delete("a", 8L);
                return db.GetLast("a");
            },
            assertOutcome: o =>
            { 
                var (pk, value) = DecodeBlockPayload(o);
                Assert.That(pk, Is.EqualTo(7L));
                Assert.That(value, Is.EqualTo(8f));
            });
        
    }
    
    [Test]
    public void when_first_and_second_are_deleted_get_first_should_return_the_third()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db,"a", new []
                {
                    (1L, 2f), (2L, 3f), (3L, 4f), //seg 0
                    (4L, 5f), (5L, 6f), (6L, 7f),  //seg 1
                    (7L, 8f), (8L, 9f), //seg 2
                });
                db.Delete("a", 1L);
                db.Delete("a", 2L);
                return db.GetFirst("a");
            },
            assertOutcome: o =>
            { 
                var (pk, value) = DecodeBlockPayload(o);
                Assert.That(pk, Is.EqualTo(3L));
                Assert.That(value, Is.EqualTo(4f));
            });
        
    }
    
    [Test]
    public void count_in_empty_db()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                return db.Count(structDef);
            },
            assertOutcome: r =>
            { 
                Assert.That(r, Is.EqualTo(0));
            });
    }

    [Test]
    public void count_single_segment_case()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db, "a", new[]
                {
                    (1L, 2f), (2L, 3f), (3L, 4f), //seg 0
                });
                return db.Count(structDef);
            },
            assertOutcome: r =>
            { 
                Assert.That(r, Is.EqualTo(3));
            });
    }
    
    [Test]
    public void count_multi_segment_case()
    {
        var structDef = new StructDef(Id: Guid.NewGuid(), Name: "a", 3,FieldsDefs);

        DbTest(
            arrange: () => Db.Create(FolderPath, DbName),
            act: db =>
            {
                db.Define(structDef);
                UpsertMany(db, "a", new[]
                {
                    (1L, 2f), (2L, 3f), (3L, 4f), //seg 0
                    (4L, 5f), (5L, 6f), (6L, 7f),  //seg 1
                    (7L, 8f), (8L, 9f), //seg 2
                    
                });
                db.Delete("a", 2L);
                return db.Count(structDef);
            },
            assertOutcome: r =>
            { 
                Assert.That(r, Is.EqualTo(7));
            });
    }
    
    public static object[] GetLastWithUpperLimitCases = {
        new object[]
        {
            new []{(1L, 2f), (2L, 3f), (3L, 4f), (4L, 5f)}, 3L, (3L, 4f),
        },
        new object[]
        {
            new[] {(1L, 2f), (3L, 4f), (4L, 5f)}, 0L, null,
        },
        new object[]
        {
            new []{(1L, 2f), (2L, 3f), (3L, 4f), (4L, 5f)}, 5L, (4L,5f),
        },
        new object[]
        {
            new []{(0L, 1f), (600L, 2f), (1_200L, 5f)},  1_799L, (1_200L, 5f),
        },
    };
    
    public static object[] GetFirstWithLowerLimitCases = {
        new object[]
        {
            new[] {(1L, 2f), (3L, 4f), (4L, 5f)}, 2L, (3L, 4f),
        },
        new object[]
        {
            new[] {(1L, 2f), (3L, 4f), (4L, 5f)}, 0L, (1L, 2f)
        },
        new object[]
        {
            new[] {(1L, 2f), (3L, 4f), (4L, 5f)}, 5L, null,
        }
    };

    private static FieldDef[] FieldsDefs { get; } = new[]
    {
        new FieldDef("at", "long", 1,8),
        new FieldDef("value", "float", 2,4),
    };

}