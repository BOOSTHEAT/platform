using ImpliciX.Language.Model;
using ImpliciX.Records.Tests.Helpers;
using ImpliciX.TestsCommon;
using NFluent;
using static ImpliciX.Language.Records.Records;
using static ImpliciX.Records.Tests.Helpers.Context;

namespace ImpliciX.Records.Tests;

public class HotStorageTests
{
    private readonly TimeHelper T = TimeHelper.Minutes();

    [Test]
    public void NominalCase()
    {
        var context = Create(new[]
        {
            Record(model.the_alarms).Is.Last(3).Snapshot.Of(model.alarm).Instance
        });

        context.Publish(model.alarm.form.some_value, 300, T._1);
        context.Publish(model.alarm.form.kind, TheAlarmKind.Error1, T._2);

        var published = context.ExecuteCommand(model.alarm.write, T._5);

        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1L, T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error1, T._5),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(300f), T._5),
        }));

        context.Publish(model.alarm.form.some_value, 350, T._6);
        context.Publish(model.alarm.form.kind, TheAlarmKind.Error2, T._7);

        published = context.ExecuteCommand(model.alarm.write, T._10);

        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 2L, T._10),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error2, T._10),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(350f), T._10),
            new DataModelValue<long>("model:the_alarms:1:_id", 1L, T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:1:kind", TheAlarmKind.Error1, T._5),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(300f), T._5),
        }));

        context.Publish(model.alarm.form.some_value, 310, T._11);
        context.Publish(model.alarm.form.kind, TheAlarmKind.Error3, T._12);

        published = context.ExecuteCommand(model.alarm.write, T._15);

        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 3L, T._15),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error3, T._15),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(310f), T._15),
            new DataModelValue<long>("model:the_alarms:1:_id", 2L, T._10),
            new DataModelValue<TheAlarmKind>("model:the_alarms:1:kind", TheAlarmKind.Error2, T._10),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(350f), T._10),
            new DataModelValue<long>("model:the_alarms:2:_id", 1L, T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:2:kind", TheAlarmKind.Error1, T._5),
            new DataModelValue<Temperature>("model:the_alarms:2:some_value", Temperature.Create(300f), T._5),
        }));

        context.Publish(model.alarm.form.some_value, 360, T._16);
        context.Publish(model.alarm.form.kind, TheAlarmKind.Error1, T._12);

        published = context.ExecuteCommand(model.alarm.write, T._20);

        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 4L, T._20),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error1, T._20),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(360f), T._20),
            new DataModelValue<long>("model:the_alarms:1:_id", 3L, T._15),
            new DataModelValue<TheAlarmKind>("model:the_alarms:1:kind", TheAlarmKind.Error3, T._15),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(310f), T._15),
            new DataModelValue<long>("model:the_alarms:2:_id", 2L, T._10),
            new DataModelValue<TheAlarmKind>("model:the_alarms:2:kind", TheAlarmKind.Error2, T._10),
            new DataModelValue<Temperature>("model:the_alarms:2:some_value", Temperature.Create(350f), T._10),
        }));
    }

    [Test]
    public void RecordsWithDifferentRetention()
    {
        var context = Create(new[]
        {
            Record(model.the_alarms).Is.Last(2).Snapshot.Of(model.alarm).Instance,
            Record(model.the_alarms_b).Is.Last(3).Snapshot.Of(model.other_alarm).Instance
        });

        //---- Writer index 0 : Update value + Write ----
        // the_alarms
        var published = context.Publish(model.alarm.form.some_value, 100, T._10)
            .ExecuteCommand(model.alarm.write, T._10);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1L, T._15),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(100f), T._10),
        }));

        // the_alarms_b
        published = context.Publish(model.other_alarm.form.some_value, 200, T._10)
            .ExecuteCommand(model.other_alarm.write, T._10);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms_b:0:_id", 1L, T._15),
            new DataModelValue<Temperature>("model:the_alarms_b:0:some_value", Temperature.Create(200f), T._10),
        }));


        //---- Writer index 1 : Update value + Write ----
        // the_alarms
        published = context.Publish(model.alarm.form.some_value, 101, T._11).ExecuteCommand(model.alarm.write, T._11);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 2L, T._11),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(101f), T._11),
            new DataModelValue<long>("model:the_alarms:1:_id", 1L, T._10),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(100f), T._10),
        }));


        // the_alarms_b
        published = context.Publish(model.other_alarm.form.some_value, 201, T._11)
            .ExecuteCommand(model.other_alarm.write, T._11);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms_b:0:_id", 2L, T._11),
            new DataModelValue<Temperature>("model:the_alarms_b:0:some_value", Temperature.Create(201f), T._11),
            new DataModelValue<long>("model:the_alarms_b:1:_id", 1L, T._10),
            new DataModelValue<Temperature>("model:the_alarms_b:1:some_value", Temperature.Create(200f), T._10),
        }));


        //---- Writer index 2 : Update value + Write ----
        // the_alarms
        published = context.Publish(model.alarm.form.some_value, 102, T._12).ExecuteCommand(model.alarm.write, T._12);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 3L, T._12),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(102f), T._12),
            new DataModelValue<long>("model:the_alarms:1:_id", 2L, T._11),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(101f), T._11),
        }));

        // the_alarms_b
        published = context.Publish(model.other_alarm.form.some_value, 202, T._10)
            .ExecuteCommand(model.other_alarm.write, T._12);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms_b:0:_id", 3L, T._12),
            new DataModelValue<Temperature>("model:the_alarms_b:0:some_value", Temperature.Create(202f), T._12),
            new DataModelValue<long>("model:the_alarms_b:1:_id", 2L, T._11),
            new DataModelValue<Temperature>("model:the_alarms_b:1:some_value", Temperature.Create(201f), T._11),
            new DataModelValue<long>("model:the_alarms_b:2:_id", 1L, T._10),
            new DataModelValue<Temperature>("model:the_alarms_b:2:some_value", Temperature.Create(200f), T._10),
        }));


        //---- Writer index 3 : Update value + Write ----
        // the_alarms
        published = context.Publish(model.alarm.form.some_value, 103, T._10).ExecuteCommand(model.alarm.write, T._13);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 4L, T._13),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(103f), T._13),
            new DataModelValue<long>("model:the_alarms:1:_id", 3L, T._12),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(102f), T._12),
        }));

        // the_alarms_b
        published = context.Publish(model.other_alarm.form.some_value, 203, T._10)
            .ExecuteCommand(model.other_alarm.write, T._13);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms_b:0:_id", 4L, T._13),
            new DataModelValue<Temperature>("model:the_alarms_b:0:some_value", Temperature.Create(203f), T._13),
            new DataModelValue<long>("model:the_alarms_b:1:_id", 3L, T._12),
            new DataModelValue<Temperature>("model:the_alarms_b:1:some_value", Temperature.Create(202f), T._12),
            new DataModelValue<long>("model:the_alarms_b:2:_id", 2L, T._11),
            new DataModelValue<Temperature>("model:the_alarms_b:2:some_value", Temperature.Create(201f), T._11),
        }));


        //---- Writer index 4 : Write only = We write again the same value from writer----
        // the_alarms
        published = context.ExecuteCommand(model.alarm.write, T._14);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 5L, T._14),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(103f), T._14),
            new DataModelValue<long>("model:the_alarms:1:_id", 4L, T._13),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(103f), T._13),
        }));

        // the_alarms_b
        published = context.ExecuteCommand(model.other_alarm.write, T._14);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms_b:0:_id", 5L, T._14),
            new DataModelValue<Temperature>("model:the_alarms_b:0:some_value", Temperature.Create(203f), T._14),
            new DataModelValue<long>("model:the_alarms_b:1:_id", 4L, T._13),
            new DataModelValue<Temperature>("model:the_alarms_b:1:some_value", Temperature.Create(203f), T._13),
            new DataModelValue<long>("model:the_alarms_b:2:_id", 3L, T._12),
            new DataModelValue<Temperature>("model:the_alarms_b:2:some_value", Temperature.Create(202f), T._12),
        }));

        published = context.ExecuteCommand(model.alarm.write, T._15);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 6L, T._15),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(103f), T._15),
            new DataModelValue<long>("model:the_alarms:1:_id", 5L, T._14),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(103f), T._14),
        }));

               // the_alarms_b
       published = context.ExecuteCommand(model.other_alarm.write, T._15);
       Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
       {
           new DataModelValue<long>("model:the_alarms_b:0:_id", 6L, T._15),
           new DataModelValue<Temperature>("model:the_alarms_b:0:some_value", Temperature.Create(203f), T._15),
           new DataModelValue<long>("model:the_alarms_b:1:_id", 5L, T._14),
           new DataModelValue<Temperature>("model:the_alarms_b:1:some_value", Temperature.Create(203f), T._14),
           new DataModelValue<long>("model:the_alarms_b:2:_id", 4L, T._13),
           new DataModelValue<Temperature>("model:the_alarms_b:2:some_value", Temperature.Create(203f), T._13),
       }));
    }

    [Test]
    public void
        GivenOneRecordWithMultipleWriters_WhenWriteCommandIsReceivedFromEachWriter_ThenRetentionStepAreIncreaseAtEachWriteCommand()
    {
        var context = Create(new[]
        {
            Record(model.the_alarms).Is.Last(3).Snapshot.Of(model.alarm, model.other_alarm).Instance
        });

        context.Publish(model.alarm.form.some_value, 10, T._1);
        context.Publish(model.alarm.form.kind, TheAlarmKind.Error2, T._10);
        context.Publish(model.other_alarm.form.some_value, 20, T._3);
        context.Publish(model.other_alarm.form.kind, TheAlarmKind.Error3, T._4);

        var published = context.ExecuteCommand(model.alarm.write, T._5);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 1L, T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error2, T._5),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(10f), T._5),
        }));

        published = context.ExecuteCommand(model.other_alarm.write, T._10);
        Assert.That(published, Is.EquivalentTo(new IDataModelValue[]
        {
            new DataModelValue<long>("model:the_alarms:0:_id", 2L, T._10),
            new DataModelValue<TheAlarmKind>("model:the_alarms:0:kind", TheAlarmKind.Error3, T._10),
            new DataModelValue<Temperature>("model:the_alarms:0:some_value", Temperature.Create(20f), T._10),
            new DataModelValue<long>("model:the_alarms:1:_id", 1L, T._5),
            new DataModelValue<TheAlarmKind>("model:the_alarms:1:kind", TheAlarmKind.Error2, T._5),
            new DataModelValue<Temperature>("model:the_alarms:1:some_value", Temperature.Create(10f), T._5),
        }));
    }
}