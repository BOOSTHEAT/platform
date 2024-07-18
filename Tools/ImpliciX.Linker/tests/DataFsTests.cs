using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ImpliciX.Data;
using ImpliciX.Linker.Values;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests;

public class DataFsTests
{
  [Test]
  public void FindExeOperations()
  {
    var package = ReadSamplePackage();
    var args = new Dictionary<string, object>()
    {
      { "manifest-file", "/tmp/the_manifest_file.json" },
      {"exe", new List<ExeInstall>
      {
        new ("devices:mmi:boiler_app,/opt/software/boiler_app"),
        new ("devices:mmi:gui,/opt/software/gui")
      }},
      {"destination", new List<string>
      {
        "/opt/slot/bootfs.0",
        "/opt/slot/bootfs.1"
      }}
    };
    var operations = DataFs.FindOperations(args, package).Select(x => x.ToString()).ToArray();
    Check.That(operations).IsEqualTo(new[]
    {
      "Copy /tmp/the_manifest_file.json to /opt/slot/bootfs.0/manifest.json",
      "Copy /tmp/the_manifest_file.json to /opt/slot/bootfs.1/manifest.json",
      "Decompress /tmp/datafs_test/content/boiler_app.zip to /opt/software/boiler_app/2022.5.17.9",
      "Link /opt/slot/bootfs.0/boiler_app to /opt/software/boiler_app/2022.5.17.9",
      "Link /opt/slot/bootfs.1/boiler_app to /opt/software/boiler_app/2022.5.17.9",
      "Copy /tmp/datafs_test/content/BOOSTHEAT.Boiler.GUI to /opt/software/gui/2022.5.17.3/BOOSTHEAT.Boiler.GUI",
      "Link /opt/slot/bootfs.0/gui to /opt/software/gui/2022.5.17.3",
      "Link /opt/slot/bootfs.1/gui to /opt/software/gui/2022.5.17.3",
    });
  }
  
  [Test]
  public void FindFileOperations()
  {
    var args = new Dictionary<string, object>()
    {
      {"file", new List<SourceAndTarget>
      {
        new ("/foo/profile,/home/.profile"),
        new ("/foo/authorized_keys,/home/.ssh/authorized_keys"),
        new ("/foo/bar.zip,/var/lib/bar")
      }}
    };
    var operations = DataFs.FindOperations(args, null).Select(x => x.ToString()).ToArray();
    Check.That(operations).IsEqualTo(new[]
    {
      "Copy /foo/profile to /home/.profile",
      "Copy /foo/authorized_keys to /home/.ssh/authorized_keys",
      "Decompress /foo/bar.zip to /var/lib/bar",
    });
  }
  
  [Test]
  public void CreateLinkOperations()
  {
    var args = new Dictionary<string, object>()
    {
      {"link", new List<SourceAndTarget>
      {
        new ("/root/foo,/root/bar/other"),
        new ("/root/bar,/root/foo/other"),
      }}
    };
    var operations = DataFs.FindOperations(args, null).Select(x => x.ToString()).ToArray();
    Check.That(operations).IsEqualTo(new[]
    {
      "Link /root/bar/other to /root/foo",
      "Link /root/foo/other to /root/bar",
    });
  }
  
    
  [Test]
  public void UpdateImageContent()
  {
    string imageContent =
      @"{
  ""name"": ""Boostheat MMI2 Image"",
  ""description"": ""Image for Boostheat MMI2 board"",
  ""version"": ""2.3.9.71"",
  ""release_date"": ""2022-03-21"",
  ""blockdevs"": [
    {
      ""partitions"": [
        {
          ""partition_size_nominal"": 512,
          ""want_maximised"": false,
          ""content"": {
            ""label"": ""ROOTFS-B"",
            ""filesystem_type"": ""ext4"",
            ""mkfs_options"": ""-E nodiscard"",
            ""filename"": ""Boostheat_image-colibri-imx7-emmc.tar.xz"",
            ""uncompressed_size"": 365.55859375
          }
        },
        {
          ""partition_size_nominal"": 512,
          ""want_maximised"": true,
          ""content"": {
            ""label"": ""DATA"",
            ""filesystem_type"": ""ext4""
          }
        }
      ]
    }
    ]
  }";
    var package = ReadSamplePackage();
    var actualImageContent = DataFs.UpdateImageContent(imageContent, new FileInfo("datafs_test.zip"), package);
    string expectedImageContent =
      @"{
  ""name"": ""BH_Insight MMI Image"",
  ""description"": ""Image for Boostheat MMI2 board"",
  ""version"": ""2.3.10.239"",
  ""release_date"": ""2022-05-17"",
  ""blockdevs"": [
    {
      ""partitions"": [
        {
          ""partition_size_nominal"": 512,
          ""want_maximised"": false,
          ""content"": {
            ""label"": ""ROOTFS-B"",
            ""filesystem_type"": ""ext4"",
            ""mkfs_options"": ""-E nodiscard"",
            ""filename"": ""Boostheat_image-colibri-imx7-emmc.tar.xz"",
            ""uncompressed_size"": 365.55859375
          }
        },
        {
          ""partition_size_nominal"": 512,
          ""want_maximised"": true,
          ""content"": {
            ""label"": ""DATA"",
            ""filesystem_type"": ""ext4"",
            ""mkfs_options"": ""-E nodiscard"",
            ""filename"": ""datafs_test.zip"",
            ""uncompressed_size"": 9
          }
        }
      ]
    }
  ]
}";
    Assert.That(actualImageContent, Is.EqualTo(expectedImageContent));
  }
  
  private static Package ReadSamplePackage()
  {
    var packagePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "datafs_test.zip");
    var args0 = new Dictionary<string, object>()
    {
      { "package", new Uri("file://" + packagePath) }
    };
    var package = DataFs.ReadPackage(args0);
    return package!;
  }

}