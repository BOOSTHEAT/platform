using System;
using System.Linq;
using ImpliciX.Data.HotTimeSeries;
using ImpliciX.Metrics.Computers;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Metrics.Tests;

[NonParallelizable]
public class PeriodicComputerServiceTest
{
    private IReadTimeSeries _tsReader;
    private IWriteTimeSeries _tsWriter;
    private readonly TimeHelper T = TimeHelper.Minutes();


    [SetUp]
    public void Init()
    {
        var dbPath = "/tmp/periodic_cmp";
        if(System.IO.Directory.Exists(dbPath))
            System.IO.Directory.Delete(dbPath, true);
        var db = new TimeSeriesDb(dbPath, "test");
        _tsReader = db;
        _tsWriter = db;
    }

    [Test]
    public void GivenInputDataIsAddedOnPublicationPeriodAt_WhenIGetInputsForPublish_ThenIGetAllValuesExceptValueOnPublicationAt()
    {
        var publicationPeriod = T._3;
        var sut = CreateSut("streamKey", publicationPeriod, publicationPeriod, T._0);

        //   T: 0 1 2 3
        // Val:   7 8 3 
        // Pub:       {7,8}

        sut.AddNewInputValue(7, T._1);
        sut.AddNewInputValue(8, T._2);
        sut.AddNewInputValue(3, publicationPeriod);

        var inputsForPublish = sut.GetInputsForPublish(publicationPeriod);
        Check.That(inputsForPublish.IsSome).IsTrue();
        var valuesForPublish = inputsForPublish.GetValue().Select(it => it.Value).ToArray();
        Check.That(valuesForPublish).ContainsExactly(7, 8);
    }

    [Test]
    public void GivenWindowed_AndInputDataIsAddedOnPublicationPeriodAt_WhenIGetInputsForPublish_ThenIGetAllValuesExceptValueOnPublicationAt()
    {
        var publicationPeriod = T._3;
        var windowPeriod = publicationPeriod * 2;
        var sut = CreateSut("streamKey", windowPeriod, publicationPeriod, T._0);

        //      T: 0   1   2   3   4   5   6   7   8   9
        //    Val:     7   8   3  10   9   1       4   33
        // Pub T3:           {7,8}
        // Pub T6:                    {7,8,3,10,9}
        // Pub T9:                                     {3,10,9,1,4}

        sut.AddNewInputValue(7, T._1);
        sut.AddNewInputValue(8, T._2);
        sut.AddNewInputValue(3, T._3);

        var inputsForPublish = sut.GetInputsForPublish(publicationPeriod);
        sut.SetSamplingStartForNextPublish(publicationPeriod);

        Check.That(inputsForPublish.IsSome).IsTrue();
        var valuesForPublish = inputsForPublish.GetValue().Select(it => it.Value).ToArray();
        Check.That(valuesForPublish).ContainsExactly(7, 8);

        sut.AddNewInputValue(10, T._4);
        sut.AddNewInputValue(9, T._5);
        sut.AddNewInputValue(1, T._6);

        inputsForPublish = sut.GetInputsForPublish(publicationPeriod * 2);
        sut.SetSamplingStartForNextPublish(publicationPeriod * 2);

        Check.That(inputsForPublish.IsSome).IsTrue();
        valuesForPublish = inputsForPublish.GetValue().Select(it => it.Value).ToArray();
        Check.That(valuesForPublish).ContainsExactly(7, 8, 3, 10, 9);

        sut.AddNewInputValue(4, T._8);
        sut.AddNewInputValue(33, T._9);

        inputsForPublish = sut.GetInputsForPublish(publicationPeriod * 3);
        sut.SetSamplingStartForNextPublish(publicationPeriod * 3);

        Check.That(inputsForPublish.IsSome).IsTrue();
        valuesForPublish = inputsForPublish.GetValue().Select(it => it.Value).ToArray();
        Check.That(valuesForPublish).ContainsExactly(3, 10, 9, 1, 4);
    }

    private PeriodicComputerService CreateSut(string streamKey, TimeSpan inputStreamPeriod, TimeSpan publicationPeriod, TimeSpan now)
        => new (streamKey, inputStreamPeriod, publicationPeriod, _tsReader, _tsWriter, now);
}