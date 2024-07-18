using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.Language.Model;
using ImpliciX.Language.Records;
using ImpliciX.Records.Tests.Helpers;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;
using static ImpliciX.Language.Records.Records;
using static ImpliciX.Records.Tests.Helpers.Context;

namespace ImpliciX.Records.Tests;

public class RecordsTests
{
    private readonly TimeHelper T = TimeHelper.Minutes();
    private Context _context;

    [SetUp]
    public void SetUp()
    {
        var record = Record(model.the_alarms).Is.Snapshot.Of(model.alarm).Instance;
        _context = Create(new[] {record});
    }

    [Test]
    public void IPublishOnlyForRecordsWhichAreDeclaredAtInitializing()
    {
        var record = Record(model.the_alarms).Is.Snapshot.Of(model.alarm).Instance;
        var context = Create(new[] {record});
        context.Publish(model.alarm.form.some_value, 10, T._1);

        var published = context.ExecuteCommand(model.other_alarm.write, T._5);
        Check.That(published).IsEmpty();
    }

    [Test]
    public void WritersInARecordMustBeUnique()
    {
        var ex = Check.ThatCode(() =>
            new RecordsService(new[]
            {
                Record(model.the_alarms).Is.Snapshot.Of(model.alarm, model.alarm_copy).Instance
            }, Mock.Of<IColdRecordsDb>(), null!,null!,null!)
        ).Throws<InvalidOperationException>().Value;

        Check.That(ex.Message).Contains("contains duplicated writers");
    }
    
    [Test]
    public void RecordsMustHaveUniqueIdentifiers()
    {
        var ex = Check.ThatCode(() =>
            new RecordsService(new[]
            {
                Record(model.the_alarms).Is.Snapshot.Of(model.alarm).Instance,
                Record(model.the_alarms).Is.Snapshot.Of(model.other_alarm).Instance

            }, Mock.Of<IColdRecordsDb>(), null!,null!, null!)
        ).Throws<InvalidOperationException>().Value;

        Check.That(ex.Message).Contains("The records must have unique identifiers");
    }

    [Test]
    public void GivenAllFormDataAreNotSet_WhenWriteCommandIsReceived_ThenIPublishOnlyDataReceived()
    {
        _context.Publish(new IDataModelValue[]
            {
                new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error3, T._5)
            }
        );

        var published = _context.ExecuteCommand(model.alarm.write, T._5);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1L, T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error3, T._5),
        }));
    }

    [Test]
    public void NominalCase()
    {
        _context.Publish(new IDataModelValue[]
        {
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(10), T._1),
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error2, T._2),
        });
        var published = _context.ExecuteCommand(model.alarm.write, T._5);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1L, T._5),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(10), T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error2, T._5),
        }));
    }

    [Test]
    public void WhenTheSameWriterIsUsedForMultipleRecords()
    {
        var context = Create(new[]
        {
            Record(model.the_alarms).Is.Snapshot.Of(model.alarm).Instance,
            Record(model.the_alarms_b).Is.Snapshot.Of(model.alarm).Instance
        });
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(10), T._1),
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error2, T._2),
        });

        var published = context.ExecuteCommand(model.alarm.write, T._5);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1L, T._5),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(10), T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error2, T._5),
            new DataModelValue<long>("model:the_alarms_b:0:_id", 1L, T._5),
            new DataModelValue<Temperature>("model:the_alarms_b:0:some_value", Temperature.Create(10), T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms_b:0:kind", TheAlarmKind.Error2, T._5),
        }));
     }

    [Test]
    public void GivenOneWriterWithMultipleUpdates_WhenWriteCommandIsReceived_ThenRecordPublishedAreEqualsToTheLastUpdates_AndCountAsOneStepOfRetention()
    {
        _context.Publish(new IDataModelValue[]
        {
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(10), T._1),
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error2, T._2),
        });
        
        _context.Publish(new IDataModelValue[]
        {
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(20), T._3),
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error3, T._4),
        });
        
        var published = _context.ExecuteCommand(model.alarm.write, T._5);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1L, T._5),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(20), T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error3, T._5),

        }));
    }

    [Test]
    public void GivenNestedProperty_WhenWriteCommandIsReceived_ThenOutputUrnHaveTheFullNestedPart()
    {
        _context.Publish(new IDataModelValue[]
        {
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(10), T._1),
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error2, T._2),
            new DataModelValue<Temperature>(model.alarm.form.nested_alarm.nested_temp, Temperature.Create(15), T._3),
        });
        var published = _context.ExecuteCommand(model.alarm.write, T._5);
        
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1L, T._5),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(10), T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error2, T._5),
            new DataModelValue<Temperature>("model:the_alarms:0:nested_alarm:nested_temp", Temperature.Create(15), T._5),
        }));
    }

    [Test]
    public void GivenRecordsWithHistory_PublishAllRecordsHistory()
    {
        var context = Create(new[]
        {
            Record(model.the_alarms).Is.Last(3).Snapshot.Of(model.alarm).Instance,
            Record(model.the_alarms_b).Is.Snapshot.Of(model.other_alarm).Instance,
        });
        
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error1, T._1),
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(10), T._1),
        });
        context.ExecuteCommand(model.alarm.write, T._1);

        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.other_alarm.form.kind, TheAlarmKind.Error2, T._2),
            new DataModelValue<Temperature>(model.other_alarm.form.some_value, Temperature.Create(20), T._2),
        });
        context.ExecuteCommand(model.other_alarm.write, T._2);

        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error3, T._3),
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(30), T._3),
        });
        context.ExecuteCommand(model.alarm.write, T._3);

        var outcome = (PropertiesChanged)context.PublishAllRecordsHistory(T._4)[0];
        Assert.That(outcome.ModelValues, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 2, T._3),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error3, T._3),
            new DataModelValue<Temperature>( "model:the_alarms:0:some_value", Temperature.Create(30), T._3),
            new DataModelValue<long>("model:the_alarms:1:_id", 1, T._1),
            new DataModelValue<TheAlarmKind>("model:the_alarms:1:kind", TheAlarmKind.Error1, T._1),
            new DataModelValue<Temperature>( "model:the_alarms:1:some_value", Temperature.Create(10), T._1),
        }));
    }

    [Test]
    public void GivenRecords_WithTextFields_NominalCase()
    {
        var context = Create(new[]
        {
            Record(model.text_record).Is.Last(3).Snapshot.Of(model.text_record_writer).Instance,
        });
        
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<Literal>(model.text_record_writer.form.literal, Literal.Create("literal"), T._1),
            new DataModelValue<Text10>(model.text_record_writer.form.text_10, Text10.Create("foo_bar_fizz_buzz"), T._1),
            new DataModelValue<Text50>(model.text_record_writer.form.text_50, Text50.Create("toto"), T._1),
            new DataModelValue<Text200>(model.text_record_writer.form.text_200, Text200.Create("the quick brown fox jumps over the lazy fox"), T._1),
        });
        var published = context.ExecuteCommand(model.text_record_writer.write, T._2);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:text_record:0:_id", 1L, T._2),
            new DataModelValue<Literal>("model:text_record:0:literal", Literal.Create("literal"), T._2),
            new DataModelValue<Text10>("model:text_record:0:text_10", Text10.Create("foo_bar_fizz_buzz"), T._2),
            new DataModelValue<Text50>("model:text_record:0:text_50", Text50.Create("toto"), T._2),
            new DataModelValue<Text200>("model:text_record:0:text_200", Text200.Create("the quick brown fox jumps over the lazy fox"), T._2),
        }));
    }

    [Test]
    public void GivenRecordsWithMultipleWriters_Record_Publishing()
    {
        var context = Create(new[]
        {
            Record(model.the_alarms).Is.Snapshot.Of(model.alarm, model.other_alarm).Instance,
        });
        
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error1, T._1),
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(10), T._1),
        });
        
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.other_alarm.form.kind, TheAlarmKind.Error2, T._2),
            new DataModelValue<Temperature>(model.other_alarm.form.some_value, Temperature.Create(20), T._2),
        });
        
        var publishedWriter1 = context.ExecuteCommand(model.alarm.write, T._3);
        Assert.That(publishedWriter1, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1, T._3),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error1, T._3),
            new DataModelValue<Temperature>( "model:the_alarms:0:some_value", Temperature.Create(10), T._3),
        }));
        
        var publishedWriter2 = context.ExecuteCommand(model.other_alarm.write, T._4);
        Assert.That(publishedWriter2, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 2, T._4),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error2, T._4),
            new DataModelValue<Temperature>( "model:the_alarms:0:some_value", Temperature.Create(20), T._4),
        }));
    }
    
    [Test]
    public void GivenRecordsWithMultipleWritersAndHistory_Record_Publishing()
    {
        var context = Create(new[]
        {
            Record(model.the_alarms).Is.Last(3).Snapshot.Of(model.alarm, model.other_alarm).Instance,
        });
        
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error1, T._1),
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(10), T._1),
        });
        
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.other_alarm.form.kind, TheAlarmKind.Error2, T._2),
            new DataModelValue<Temperature>(model.other_alarm.form.some_value, Temperature.Create(20), T._2),
        });
        
        var publishedWriter1 = context.ExecuteCommand(model.alarm.write, T._3);
        Assert.That(publishedWriter1, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1, T._3),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error1, T._3),
            new DataModelValue<Temperature>( "model:the_alarms:0:some_value", Temperature.Create(10), T._3),
        }));
        
        var publishedWriter2 = context.ExecuteCommand(model.other_alarm.write, T._4);
        Assert.That(publishedWriter2, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 2, T._4),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error2, T._4),
            new DataModelValue<Temperature>( "model:the_alarms:0:some_value", Temperature.Create(20), T._4),
            new DataModelValue<long>("model:the_alarms:1:_id", 1, T._3),
            new DataModelValue<TheAlarmKind>("model:the_alarms:1:kind", TheAlarmKind.Error1, T._3),
            new DataModelValue<Temperature>( "model:the_alarms:1:some_value", Temperature.Create(10), T._3),
        }));
    }
    
    [Test]
    public void GivenRecordsWithMultipleWriters_Record_Publishing_ConcurrentWrites()
    {
        var context = Create(new[]
        {
            Record(model.the_alarms).Is.Snapshot.Of(model.alarm, model.other_alarm).Instance,
        });
        
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.alarm.form.kind, TheAlarmKind.Error1, T._1),
            new DataModelValue<Temperature>(model.alarm.form.some_value, Temperature.Create(10), T._1),
        });
        
        context.Publish(new IDataModelValue[]
        {
            new DataModelValue<TheAlarmKind>(model.other_alarm.form.kind, TheAlarmKind.Error2, T._2),
            new DataModelValue<Temperature>(model.other_alarm.form.some_value, Temperature.Create(20), T._2),
        });
        
        var publishedWriter1 = context.ExecuteCommand(model.alarm.write, T._3);
        Assert.That(publishedWriter1, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1, T._3),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error1, T._3),
            new DataModelValue<Temperature>( "model:the_alarms:0:some_value", Temperature.Create(10), T._3),
        }));
        
        var publishedWriter2 = context.ExecuteCommand(model.other_alarm.write, T._3);
        Assert.That(publishedWriter2, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 2, T._3),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error2, T._3),
            new DataModelValue<Temperature>( "model:the_alarms:0:some_value", Temperature.Create(20), T._3),
        }));
    }
}