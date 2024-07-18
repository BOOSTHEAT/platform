using ImpliciX.Data.Records;
using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.Language.Model;
using ImpliciX.Language.Records;
using ImpliciX.Records.Tests.Helpers;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;
using static ImpliciX.Language.Records.Records;
using static ImpliciX.Records.Tests.Helpers.Context;

namespace ImpliciX.Records.Tests;

public class ColdStorageTests
{
    private readonly TimeHelper T = TimeHelper.Minutes();
    private Context _context;
    private Mock<IColdRecordsDb> _dbMock;

    [SetUp]
    public void Setup()
    {
        _dbMock = new Mock<IColdRecordsDb>();
        _context = Create(new[]
        {
            Record(model.the_alarms).Is.Snapshot.Of(model.alarm).Instance
        }, _dbMock.Object);
    }

    [Test]
    public void NominalCase()
    {
        _context.Publish(model.alarm.form.some_value, 300, T._1);
        _context.Publish(model.alarm.form.kind, TheAlarmKind.Error1, T._2);

        _ = _context.ExecuteCommand(model.alarm.write, T._5);

        var expected = new[]
        {
            CreateFloatProperty("some_value", 300, T._5),
            CreateEnumProperty("kind", TheAlarmKind.Error1, T._5)
        };
        
        _dbMock.Verify(db=>db.Write(It.Is<Snapshot>(s=> VerifyDataToStore(s, expected))), Times.Once);
    }

    bool VerifyDataToStore(Snapshot snapshot, IDataModelValue[] expected)
    {
        var mvs = snapshot.Values;
        for (var i = 0; i < mvs.Length; i++)
        {
            Check.That(mvs[i].Urn).IsEqualTo(expected[i].Urn);
            Check.That(mvs[i].ModelValue()).IsEqualTo(expected[i].ModelValue());
            Check.That(mvs[i].At).IsEqualTo(expected[i].At);
        }
        return true;
    }
}