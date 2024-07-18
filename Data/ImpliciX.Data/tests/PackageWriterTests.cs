using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Data.Tests
{
  [Platform(Include = "Linux")]
  public class PackageWriterTests
  {
    [Test]
    public void write_nominal_package()
    {
      var manifest = new Manifest
      {
        Device = "BOOSTHEAT.20_V2",
        Revision = "9.9.9.999",
        Date = new DateTime(2021,8,16,8,35,6),
        Content = new Manifest.ContentData
        {
          APPS = Enumerable.Empty<Manifest.PartData>().ToArray(),
          MCU = new []
          {
            new Manifest.PartData
            {
              Target = "mcu_iu",
              Revision = "1999.12.31.1",
              FileName = "Carte_HAUT.bin"
            },
            new Manifest.PartData
            {
              Target = "mcu_heat_pump",
              Revision = "1999.12.31.1",
              FileName = "Carte_BAS.bin"
            },
            new Manifest.PartData
            {
              Target = "mcu_eu",
              Revision = "1999.12.31.1",
              FileName = "Carte_UE.bin"
            }
          },
          BSP = Enumerable.Empty<Manifest.PartData>().ToArray(),
        }
      };
      var content = Directory.EnumerateFiles("package_examples/writer_nominal_case_content");
      var outputFile = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
      var result = PackageWriter.Write(manifest, content, outputFile, new []{("./test_artefact","GUI_SRC")});

      var softwareDeviceMap = new Dictionary<string, SoftwareDeviceNode>
      {
        ["mcu_eu"] = dummy.mcu1,
        ["mcu_iu"] = dummy.mcu2,
        ["mcu_heat_pump"] = dummy.mcu3
      };
      var actualPackage = PackageLoader.Load(PackageLocation.FromString($"file://{outputFile}").Value, k => softwareDeviceMap[k]);
      var expectedPackage = PackageLoader.Load(PackageLocation.FromString($"file://package_examples/writer_nominal_case.zip").Value, k => softwareDeviceMap[k]);
      
      Assert.IsTrue(actualPackage.IsSuccess, actualPackage.Error?.Message);
      Assert.IsTrue(expectedPackage.IsSuccess, expectedPackage.Error?.Message);
      Check.That(actualPackage.Value.ApplicationName).IsEqualTo(expectedPackage.Value.ApplicationName);
      Check.That(actualPackage.Value.Revision).IsEqualTo(expectedPackage.Value.Revision);
      Check.That(actualPackage.Value.Date).IsEqualTo(expectedPackage.Value.Date);
      Check.That(result.IsSuccess).IsTrue();
      foreach (var node in softwareDeviceMap.Values)
      {
        var actualContent = actualPackage.Value._contents[node];
        var expectedContent = expectedPackage.Value._contents[node];
        Check.That(actualContent.Bytes).IsEqualTo(expectedContent.Bytes);
      }
    }
  }
}