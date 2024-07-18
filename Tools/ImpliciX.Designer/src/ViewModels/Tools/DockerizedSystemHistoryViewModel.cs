using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using ImpliciX.DesktopServices;
using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using File = System.IO.File;

namespace ImpliciX.Designer.ViewModels.Tools;

public class DockerizedSystemHistoryViewModel : ActionMenuViewModel<IConcierge>
{
  private const string ContainerName = "bhSystemHistory7713";
  private const string ImageName = "influxdb:1.8";
  private const string LocalPort = "7713";

  public DockerizedSystemHistoryViewModel(
    IConcierge concierge
  ) : base(concierge)
  {
    Text = "Load System History Backup...";
  }

  public override async void Open()
  {
    var folder = await Concierge.User.OpenFolder(
      new IUser.FileSelection
      {
        Title = "Load System History Backup"
      }
    );
    if (folder.Choice != IUser.ChoiceType.Ok)
      return;
    await BusyWhile(
      async () =>
      {
        try
        {
          await LaunchInfluxdbServer();
          var dataset = LoadCollectdData(folder.Path);
          await WriteToInflux(dataset);
          Concierge.Console.WriteLine("System History import complete");
        }
        catch (Exception e)
        {
          await Errors.Display(e);
        }
      }
    );
  }

  private async Task WriteToInflux(
    IEnumerable<(string category, string measure, XDocument content)> dataset
  )
  {
    using var influxClient = InfluxDBClientFactory.CreateV1(
      $"http://127.0.0.1:{LocalPort}",
      "",
      "".ToCharArray(),
      "collectd",
      "autogen"
    );
    var writer = influxClient.GetWriteApiAsync();
    foreach (var (category, measure, rrd) in dataset)
    {
      Concierge.Console.WriteLine($"Importing {category} / {measure}");
      var measurement = PointData.Measurement(category);
      var dump = new RoundRobinDump(rrd);
      var loaded = dump.Load();
      foreach (var data in loaded)
      {
        var taggedMeasurement = measurement
          .Tag(
            "function",
            data.Key.Function
          )
          .Tag(
            "window",
            data.Key.Window.ToString()
          );
        var points = data.Value.Select(
          x =>
            taggedMeasurement.Timestamp(
              x.Item1,
              WritePrecision.S
            ).Field(
              $"{measure}.{data.Key.Name}",
              x.Item2
            )
        ).ToList();
        await writer.WritePointsAsync(points);
      }
    }
  }

  private async Task LaunchInfluxdbServer()
  {
    Concierge.Console.WriteLine("Launching InfluxDB server");
    var docker = Concierge.Docker;
    await docker.Stop(ContainerName);
    await docker.Pull(ImageName);
    await docker.Launch(
      ImageName,
      ContainerName,
      true,
      IDockerService.DefinePortBindings(("8086/tcp", "0.0.0.0", LocalPort))
    );
    Concierge.Console.WriteLine("Creating bucket");
    await docker.Execute(
      ContainerName,
      "influx",
      "-execute",
      "create database collectd"
    );
    Concierge.Console.WriteLine("Bucket created. InfluxDB server available.");
  }

  private IEnumerable<(string category, string measure, XDocument content)> LoadCollectdData(
    string folder
  )
  {
    XDocument GzipXmlDecompress(
      string gzipFilePath
    )
    {
      Concierge.Console.WriteLine($"Loading {gzipFilePath}");
      using var compressedFileStream = File.Open(
        gzipFilePath,
        FileMode.Open
      );
      using var decompressor = new GZipStream(
        compressedFileStream,
        CompressionMode.Decompress
      );
      using var uncompressedStream = new MemoryStream();
      decompressor.CopyTo(uncompressedStream);
      uncompressedStream.Seek(
        0,
        SeekOrigin.Begin
      );
      var doc = XDocument.Load(uncompressedStream);
      return doc;
    }

    var datas =
      from subfolder in Directory.EnumerateDirectories(folder)
      let category = Path.GetFileName(subfolder)
      from measureFile in Directory.EnumerateFiles(subfolder)
      let measure = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(Path.GetFileName(measureFile)))
      let content = GzipXmlDecompress(measureFile)
      select (category, measure, content);
    return datas;
  }
}
