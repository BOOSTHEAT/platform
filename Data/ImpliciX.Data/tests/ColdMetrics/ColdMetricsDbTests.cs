using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Data.ColdDb;
using ImpliciX.Data.ColdMetrics;
using ImpliciX.Language.Model;
using NUnit.Framework;
using PHD = ImpliciX.TestsCommon.PropertyDataHelper;

namespace ImpliciX.Data.Tests.ColdMetrics;

[NonParallelizable]
public class ColdMetricsDbTests
{
    private static readonly string StorageFolderPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(),"cold_store");

    [SetUp]
    public void Setup()
    {
        
        if (Directory.Exists(StorageFolderPath))
            Directory.Delete(StorageFolderPath, true);
        
    }

    [Test]
    public void it_should_start_new_file()
    {
        using var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar"}, StorageFolderPath);
        Assert.That(File.Exists(sut.CurrentFiles[0]), Is.True);
    }

    [Test]
    public void it_should_store_series()
    {
        using var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
        var data = new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
    
        sut.WriteMany("foo:bar:fizz", data);
        sut.Dispose();
        var dataPoints = ExtractDataPoints(sut.CurrentFiles[0]);
        Assert.Multiple(() =>
        {
            Assert.That(dataPoints, Has.Count.EqualTo(1));
            Assert.That(dataPoints[0].At, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(dataPoints[0].Values, Is.EquivalentTo(new[] {new DataPointValue("foo:bar:fizz", 1f)}));
        });
    }
    
    [Test]
    public void it_should_store_series_in_chronological_order()
    {
        using var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
        var data = new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 2, TimeSpan.Zero, TimeSpan.FromSeconds(2)),
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromSeconds(1))
        };
    
        sut.WriteMany("foo:bar:fizz", data);
        sut.Dispose();
        var dataPoints = ExtractDataPoints(sut.CurrentFiles[0]);
        Assert.Multiple(() =>
        {
            Assert.That(dataPoints.Count, Is.EqualTo(2));
            Assert.That(dataPoints[0].At, Is.EqualTo(TimeSpan.FromSeconds(1)));
            Assert.That(dataPoints[1].At, Is.EqualTo(TimeSpan.FromSeconds(2)));
        });
    }
    [Test]
    public void it_should_create_one_file_by_day_after_reloading()
    {
        var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
    
        sut.WriteMany("foo:bar:fizz",new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromHours(1)),
            PHD.CreateMetricValueProperty("foo:bar:fizz", 2, TimeSpan.Zero, TimeSpan.FromHours(2))
        });
    
        sut.Dispose();
        sut =  ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
        sut.WriteMany("foo:bar:fizz",new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 3, TimeSpan.Zero, TimeSpan.FromHours(25)),
        });
        sut.Dispose();
        var finishedFiles = sut.FinishedFiles;
    
        Assert.Multiple(() =>
        {
            Assert.That(finishedFiles.Length, Is.EqualTo(1));
            Assert.That(ExtractDataPoints(sut.CurrentFiles[0]), Has.Count.EqualTo(1));
        });
        
    }
    
    [Test]
    public void it_should_create_one_file_by_day_even_if_there_is_no_initial_data()
    {
        var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
        var data = new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromHours(1)),
            PHD.CreateMetricValueProperty("foo:bar:fizz", 2, TimeSpan.Zero, TimeSpan.FromHours(2)),
            PHD.CreateMetricValueProperty("foo:bar:fizz", 3, TimeSpan.Zero, TimeSpan.FromHours(25))
        };
    
        sut.WriteMany("foo:bar:fizz", data);
        sut.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(sut.FinishedFiles.Length, Is.EqualTo(1));
            Assert.That(sut.FinishedFiles[0], Does.Contain(".metrics.zip"));
            Assert.That(ExtractDataPoints(sut.CurrentFiles[0]), Has.Count.EqualTo(1));
        });
    }
    
    [Test]
    public void the_rotation_policy_can_be_activated_at_each_shutdown()
    {
        var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"},
            StorageFolderPath, rotatePolicy:new OneCompressedFileByDayAndBySession<MetricsDataPoint>());
        var data = new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromHours(1))
        };
    
        sut.WriteMany("foo:bar:fizz", data);
        sut.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(sut.FinishedFiles.Length, Is.EqualTo(1));
            Assert.That(sut.FinishedFiles[0], Does.Contain(".metrics.zip"));
        });
    }

    [Test]
    [Category("ExcludeWindows")]
    public void it_is_able_to_continue_with_a_moved_file()
    {
        var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
        sut.WriteMany("foo:bar:fizz", new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromHours(1)),
        });
    
        foreach (var file in Directory.EnumerateFiles(StorageFolderPath))
        {
            File.Move(file, file+".moved.metrics");
        }
        sut.WriteMany("foo:bar:fizz",new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 3, TimeSpan.Zero, TimeSpan.FromHours(1)),
        });
        sut.Dispose();
        Assert.That(ExtractDataPoints(Directory.EnumerateFiles(StorageFolderPath).First()), Has.Count.EqualTo(2));
    }

    [Test]
    [Category("ExcludeWindows")]
    public void it_is_able_to_continue_with_a_read_only_file()
    {
        var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
        var filePath = Directory.EnumerateFiles(StorageFolderPath).First();
        
        sut.WriteMany("foo:bar:fizz", new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromHours(1)),
        });
        
        File.SetAttributes(filePath, FileAttributes.ReadOnly|FileAttributes.Normal);
        
        sut.WriteMany("foo:bar:fizz",new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 3, TimeSpan.Zero, TimeSpan.FromHours(1)),
        });
        
        sut.Dispose();

        File.SetAttributes(filePath, FileAttributes.Normal);
        Assert.That(ExtractDataPoints(filePath), Has.Count.EqualTo(2));
    }

    [Test]
    public void it_is_able_to_continue_with_an_existing_file()
    {
        var sut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
        sut.WriteMany("foo:bar:fizz", new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromHours(1)),
        });
    
        sut.Dispose();
        var newSut = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"}, StorageFolderPath);
        newSut.WriteMany("foo:bar:fizz",new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 3, TimeSpan.Zero, TimeSpan.FromHours(1)),
        });
        newSut.Dispose();
        Assert.That(ExtractDataPoints(sut.CurrentFiles[0]), Has.Count.EqualTo(2));
    }

    [Test]
    public void it_loads_cold_stores_from_exising_files()
    {
        Urn[] defs = {"foo:bar", "zoo:bar", "boo:bar"};
    
        var sut1 = ColdMetricsDb.LoadOrCreate(defs,StorageFolderPath);
        sut1.WriteMany("foo:bar", new[]
        {
            PHD.CreateMetricValueProperty("foo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromHours(1)),
        });
    
        sut1.Dispose();
    
        var sut2 = ColdMetricsDb.LoadOrCreate(defs, StorageFolderPath);
        sut2.WriteMany("zoo:bar",new[]
        {
            PHD.CreateMetricValueProperty("zoo:bar:fizz", 1, TimeSpan.Zero, TimeSpan.FromHours(1)),
        });
    
        sut2.Dispose();
    
        var sut3 = ColdMetricsDb.LoadOrCreate(defs, StorageFolderPath);
        Assert.That(sut3.CurrentFiles, Has.Length.EqualTo(3));
        sut3.Dispose();
        
        Assert.That(ExtractDataPoints(sut3.CurrentFiles, "foo:bar"), Has.Count.EqualTo(1));
        Assert.That(ExtractDataPoints(sut3.CurrentFiles, "zoo:bar"), Has.Count.EqualTo(1));
        Assert.That(ExtractDataPoints(sut3.CurrentFiles, "boo:bar"), Has.Count.EqualTo(0));
    }
    
    [Test]
    public void in_safe_mode_it_puts_corrupted_files_in_quarantine_and_dont_throw()
    {
        Directory.CreateDirectory(StorageFolderPath);
        File.WriteAllBytes(Path.Combine(StorageFolderPath, "corrupted_file.metrics"),"toto"u8.ToArray());
        Assert.DoesNotThrow(()=>{
            using var c = ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"},StorageFolderPath,safeLoad:true);
            });
        Assert.IsTrue(File.Exists(Path.Combine(StorageFolderPath, "quarantine", "corrupted_file.metrics")));
    }
    
    [Test]
    public void in_failfast_mode_it_puts_corrupted_files_in_quarantine_and_throw()
    {
        Directory.CreateDirectory(StorageFolderPath);
        File.WriteAllBytes(Path.Combine(StorageFolderPath, "corrupted_file.metrics"),"toto"u8.ToArray());
        Assert.Throws(Is.AssignableTo(typeof(Exception)), ()=>ColdMetricsDb.LoadOrCreate(new Urn[]{"foo:bar:fizz"},StorageFolderPath,safeLoad:false));
        Assert.IsTrue(File.Exists(Path.Combine(StorageFolderPath, "quarantine", "corrupted_file.metrics")));
    }

    private static List<MetricsDataPoint> ExtractDataPoints(string filePath)
    {
        using var c = ColdMetricsDb.LoadCollection(filePath);
        return c.DataPoints.ToList();
    } 
    
    private static List<MetricsDataPoint> ExtractDataPoints(string[] files, string urn)
    {
        var results = new List<MetricsDataPoint>();
        foreach(var f in files){
            using var c = ColdMetricsDb.LoadCollection(f);
            if(c.MetaData.Urn==urn){
                results.AddRange(c.DataPoints);
                break;
            }    
        }
        return results;

    }
    
}