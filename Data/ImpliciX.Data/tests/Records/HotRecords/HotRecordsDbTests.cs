using System;
using System.IO;
using System.Linq;
using ImpliciX.Data.HotDb;
using ImpliciX.Data.HotDb.Model;
using ImpliciX.Data.Records;
using ImpliciX.Data.Records.HotRecords;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Language.Records;
using Moq;
using NUnit.Framework;
using static ImpliciX.Language.Records.Records;
using static ImpliciX.Data.Records.HotRecords.HotRecordsDb;

namespace ImpliciX.Data.Tests.Records.HotRecords;

[Platform(Include = "Linux")]
public class HotRecordsDbTests
{
    const string FolderPath = "/tmp/records_db";
    const string DbName = "records";
    private IRecord[] DefinedRecords { get; } = {
        Record(model.the_alarms).Is.Last(3).Snapshot.Of(model.alarm).Instance,
        Record(model.the_alarms_b).Is.Snapshot.Of(model.other_alarm).Instance
    };
    
    [SetUp]
    public void Setup()
    {
        if (Directory.Exists(FolderPath))
            Directory.Delete(FolderPath, true);
    }
    
    [Test]
    public void db_create_nominal()
    { 
        var sut = new HotRecordsDb(DefinedRecords, FolderPath, DbName);
        Assert.That(Directory.Exists(FolderPath), Is.True);
        Assert.That(Directory.EnumerateFiles(FolderPath).Any(), Is.True);
       
        Assert.That(sut.DefinedStructs.Length, Is.EqualTo(1));
        
        Assert.That(sut.DefinedStructs[0].Name, Is.EqualTo(model.the_alarms.Urn.Value));
        Assert.That(sut.DefinedStructs[0].BlocksPerSegment, Is.EqualTo(3));
        Assert.That(sut.DefinedStructs[0].Fields, Is.EquivalentTo(new FieldDef[]
        {
            new ("_id", typeof(long).FullName!, (byte) StdFields.StorageType.Long, 8),
            new ("at", typeof(long).FullName!, (byte) StdFields.StorageType.Long, 8),
            new ("kind", typeof(TheAlarmKind).FullName!, (byte) StdFields.StorageType.Float,4),
            new ("float_value", typeof(Temperature).FullName!, (byte) StdFields.StorageType.Float,4),
            new ("text_value", typeof(Literal).FullName!, (byte) StdFields.StorageType.Text,800),
            new ("nested_alarm:nested_temp", typeof(Temperature).FullName!, (byte) StdFields.StorageType.Float,4),
            new ("nested_alarm:text_value", typeof(Literal).FullName!,(byte) StdFields.StorageType.Text, 800),
        }));
    }
    
    [Test]
    public void db_create_without_records_definitions_is_forbidden()
    { 
        // ReSharper disable once ObjectCreationAsStatement
        Assert.Throws<InvalidOperationException>(()=> new HotRecordsDb(Array.Empty<IRecord>(), FolderPath, DbName));
    }

    [Test]
    public void db_write_read_nominal_case()
    {
        var sut = new HotRecordsDb(DefinedRecords, FolderPath, DbName);
        var snapshot = new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.Zero),
            new DataModelValue<Temperature>("float_value", Temperature.Create(12f), TimeSpan.Zero),
            new DataModelValue<Literal>("text_value", Literal.Create("hello"), TimeSpan.Zero),
            new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(12f), TimeSpan.Zero),
            new DataModelValue<Literal>("nested_alarm:text_value", Literal.Create("hello"), TimeSpan.Zero),
        }, TimeSpan.Zero);
        sut.Write(snapshot);
        var snapshots = sut.ReadAll(model.the_alarms.Urn);
        Assert.That(snapshots.Count, Is.EqualTo(1));
        Assert.That(snapshots[0].RecordUrn, Is.EqualTo(model.the_alarms.Urn));
        Assert.That(snapshots[0].Id, Is.EqualTo(1));
        Assert.That(snapshots[0].At, Is.EqualTo(TimeSpan.Zero));
        Assert.That(snapshots[0].Values, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.Zero),
            new DataModelValue<Temperature>("float_value", Temperature.Create(12f), TimeSpan.Zero),
            new DataModelValue<Literal>("text_value", Literal.Create("hello"), TimeSpan.Zero),
            new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(12f), TimeSpan.Zero),
            new DataModelValue<Literal>("nested_alarm:text_value", Literal.Create("hello"), TimeSpan.Zero),
        }));
    }

    [Test]
    public void db_write_many_then_read_all()
    {
        var sut = new HotRecordsDb(DefinedRecords, FolderPath, DbName);
        for (var i = 0; i < 3; i++)
        {
            var snapshot = new Snapshot(i, model.the_alarms.Urn, new IIMutableDataModelValue[]
            {
                new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.FromSeconds(i)),
                new DataModelValue<Temperature>("float_value", Temperature.Create(i), TimeSpan.FromSeconds(i)),
                new DataModelValue<Literal>("text_value", Literal.Create($"hello_{i}"), TimeSpan.FromSeconds(i)),
                new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(i*2), TimeSpan.FromSeconds(i)),
                new DataModelValue<Literal>("nested_alarm:text_value", Literal.Create($"hello_nested_{i}"), TimeSpan.FromSeconds(i)),
            }, TimeSpan.FromSeconds(i));
            sut.Write(snapshot);
        }
        var snapshots = sut.ReadAll(model.the_alarms.Urn);
        Assert.That(snapshots.Count, Is.EqualTo(3));

        for (var i = 0; i < 3; i++)
        {
            var snapshotExpected = new Snapshot(i, model.the_alarms.Urn, new IIMutableDataModelValue[]
            {
                new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.FromSeconds(i)),
                new DataModelValue<Temperature>("float_value", Temperature.Create(i), TimeSpan.FromSeconds(i)),
                new DataModelValue<Literal>("text_value", Literal.Create($"hello_{i}"), TimeSpan.FromSeconds(i)),
                new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(i*2), TimeSpan.FromSeconds(i)),
                new DataModelValue<Literal>("nested_alarm:text_value", Literal.Create($"hello_nested_{i}"), TimeSpan.FromSeconds(i)),
            }, TimeSpan.FromSeconds(i));
            
            Assert.That(snapshots[i].RecordUrn, Is.EqualTo(snapshotExpected.RecordUrn));
            Assert.That(snapshots[i].At, Is.EqualTo(snapshotExpected.At));
            Assert.That(snapshots[i].Values, Is.EquivalentTo(snapshotExpected.Values));
        }
    }

    [Test]
    public void db_write_incomplete_record()
    {
        var sut = new HotRecordsDb(DefinedRecords, FolderPath, DbName);
        var snapshot = new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.Zero),
            new DataModelValue<Literal>("text_value", Literal.Create("hello"), TimeSpan.Zero),
            new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(12f), TimeSpan.Zero),
        }, TimeSpan.Zero);
        sut.Write(snapshot);
        var snapshots = sut.ReadAll(model.the_alarms.Urn);
        Assert.That(snapshots.Count, Is.EqualTo(1));
        Assert.That(snapshots[0].RecordUrn, Is.EqualTo(model.the_alarms.Urn));
        Assert.That(snapshots[0].At, Is.EqualTo(TimeSpan.Zero));
        Assert.That(snapshots[0].Values, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.Zero),
            new DataModelValue<Literal>("text_value", Literal.Create("hello"), TimeSpan.Zero),
            new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(12f), TimeSpan.Zero),
        })); 
    }

    [Test]
    public void db_apply_retention()
    {
        var sut = new HotRecordsDb(DefinedRecords, FolderPath, DbName);
        for (var i = 0; i < 5; i++)
        {
            var snapshot = new Snapshot(i, model.the_alarms.Urn, new IIMutableDataModelValue[]
            {
                new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.FromSeconds(i)),
                new DataModelValue<Temperature>("float_value", Temperature.Create(i), TimeSpan.FromSeconds(i)),
                new DataModelValue<Literal>("text_value", Literal.Create($"hello_{i}"), TimeSpan.FromSeconds(i)),
                new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(i*2), TimeSpan.FromSeconds(i)),
                new DataModelValue<Literal>("nested_alarm:text_value", Literal.Create($"hello_nested_{i}"), TimeSpan.FromSeconds(i)),
            }, TimeSpan.FromSeconds(i));
            sut.Write(snapshot);
        }
        var snapshots = sut.ReadAll(model.the_alarms.Urn);
        Assert.That(snapshots.Count, Is.EqualTo(3));
        for (var i = 0; i < 3; i++)
        {
            var j = i+2;
            var snapshot = new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
            {
                new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.FromSeconds(i)),
                new DataModelValue<Temperature>("float_value", Temperature.Create(j), TimeSpan.FromSeconds(i)),
                new DataModelValue<Literal>("text_value", Literal.Create($"hello_{j}"), TimeSpan.FromSeconds(i)),
                new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(j*2), TimeSpan.FromSeconds(i)),
                new DataModelValue<Literal>("nested_alarm:text_value", Literal.Create($"hello_nested_{j}"), TimeSpan.FromSeconds(i)),
            }, TimeSpan.FromSeconds(i));
            sut.Write(snapshot);
        }
    }

    [Test]
    public void db_load_after_dispose()
    {
        var sut = new HotRecordsDb(DefinedRecords, FolderPath, DbName);
        var snapshot = new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.Zero),
            new DataModelValue<Literal>("text_value", Literal.Create("hello"), TimeSpan.Zero),
            new DataModelValue<Temperature>("nested_alarm:nested_temp", Temperature.Create(12f), TimeSpan.Zero),
        }, TimeSpan.Zero);
        sut.Write(snapshot);
        sut.Dispose();
        sut = new HotRecordsDb(DefinedRecords, FolderPath, DbName);
        var snapshots = sut.ReadAll(model.the_alarms.Urn);
        Assert.That(snapshots.Count, Is.EqualTo(1));
    }

    [Test]
    public void support_record_definition_changes_add_one_field()
    {
        IRecord[] initialRecords = {
            Mock.Of<IRecord>(r=>r.Urn == model.the_alarms.Urn 
                                && r.Retention == Option<int>.Some(3)
                                && r.Type == typeof(TheAlarmTwoFields)),
        };
        var sut = new HotRecordsDb(initialRecords, FolderPath, DbName);
        sut.Write(new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.Zero),
            new DataModelValue<Temperature>("float_value", Temperature.Create(12f), TimeSpan.Zero),
        }, TimeSpan.Zero));
        sut.Dispose();
        
        IRecord[] newRecords = {
            Mock.Of<IRecord>(r=>r.Urn == model.the_alarms.Urn 
                                && r.Retention == Option<int>.Some(3)
                                && r.Type == typeof(TheAlarmThreeFields)),
        };
        sut = new HotRecordsDb(newRecords, FolderPath, DbName);
        sut.Write(new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.FromMinutes(1)),
            new DataModelValue<Temperature>("float_value", Temperature.Create(42f), TimeSpan.FromMinutes(1)),
            new DataModelValue<Literal>("text_value", Literal.Create("hello"), TimeSpan.FromMinutes(1)),
         }, TimeSpan.FromMinutes(1)));

        var actual = sut.ReadAll(model.the_alarms.Urn);
        Assert.That(actual.Count, Is.EqualTo(2));
    }
    
    [Test]
    public void support_record_definition_changes_remove_one_field()
    {
        IRecord[] initialRecords = {
            Mock.Of<IRecord>(r=>r.Urn == model.the_alarms.Urn 
                                && r.Retention == Option<int>.Some(3)
                                && r.Type == typeof(TheAlarmThreeFields)),
        };
        var sut = new HotRecordsDb(initialRecords, FolderPath, DbName);
        sut.Write(new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.Zero),
            new DataModelValue<Temperature>("float_value", Temperature.Create(12f), TimeSpan.Zero),
            new DataModelValue<Literal>("text_value", Literal.Create("hello"), TimeSpan.Zero),
        }, TimeSpan.Zero));
        sut.Dispose();
        
        IRecord[] newRecords = {
            Mock.Of<IRecord>(r=>r.Urn == model.the_alarms.Urn 
                                && r.Retention == Option<int>.Some(3)
                                && r.Type == typeof(TheAlarmTwoFields))
        };
        sut = new HotRecordsDb(newRecords, FolderPath, DbName);
        sut.Write(new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.FromMinutes(1)),
            new DataModelValue<Temperature>("float_value", Temperature.Create(42f), TimeSpan.FromMinutes(1)),
            
        }, TimeSpan.FromMinutes(1)));

        var actual = sut.ReadAll(model.the_alarms.Urn);
        Assert.That(actual.Count, Is.EqualTo(2));
    }

    [Test]
    public void support_adding_record_definition()
    {
        IRecord[] initialRecords = {
            Mock.Of<IRecord>(r=>r.Urn == model.the_alarms.Urn 
                                && r.Retention == Option<int>.Some(3)
                                && r.Type == typeof(TheAlarmThreeFields)),
        };
        var sut = new HotRecordsDb(initialRecords, FolderPath, DbName);
        sut.Write(new Snapshot(1, model.the_alarms.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.Zero),
            new DataModelValue<Temperature>("float_value", Temperature.Create(12f), TimeSpan.Zero),
            new DataModelValue<Literal>("text_value", Literal.Create("hello"), TimeSpan.Zero),
        }, TimeSpan.Zero));
        sut.Dispose();
        
        IRecord[] newRecords = {
            Mock.Of<IRecord>(r=>r.Urn == model.the_alarms.Urn 
                                && r.Retention == Option<int>.Some(3)
                                && r.Type == typeof(TheAlarmThreeFields)),
            
            Mock.Of<IRecord>(r=>r.Urn == model.the_alarms_b.Urn 
                                && r.Retention == Option<int>.Some(3)
                                && r.Type == typeof(TheAlarmTwoFields))
        };
        sut = new HotRecordsDb(newRecords, FolderPath, DbName);
        sut.Write(new Snapshot(1, model.the_alarms_b.Urn, new IIMutableDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>("kind", TheAlarmKind.Error1, TimeSpan.FromMinutes(1)),
            new DataModelValue<Temperature>("float_value", Temperature.Create(42f), TimeSpan.FromMinutes(1)),
            
        }, TimeSpan.FromMinutes(1)));

        Assert.That(sut.ReadAll(model.the_alarms.Urn).Count, Is.EqualTo(1));
        Assert.That(sut.ReadAll(model.the_alarms_b.Urn).Count, Is.EqualTo(1));
    }
}