using System;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Data.Tests
{
  public class ManifestTests
  {
    [Test]
    public void read_from_file()
    {
      var manifestRead = Manifest.FromFile("package_examples/nominal_manifest.json");
      Check.That(manifestRead.IsSuccess).IsTrue();
      var manifest = manifestRead.Value;
      Check.That(manifest.Device).IsEqualTo("BOOSTHEAT.20_V2");
      Check.That(manifest.Revision).IsEqualTo("2.3.4.613");
      Check.That(manifest.Date).IsEqualTo(new DateTime(2021,08,23,10,04,24));
      Check.That(manifest.SHA256).IsEqualTo("a7c0f2cafd266277c2987d0e88a0a384b402224548806aee9f5734627cec0407");
      Check.That(manifest.Content.APPS).IsEquivalentTo(new []
        {
          new Manifest.PartData()
          {
            Revision = "2021.8.23.1",
            Target = "boiler_app",
            FileName = "boiler_app.zip",
          },
          new Manifest.PartData()
          {
            Revision = "2021.8.6.1",
            Target = "boiler_gui",
            FileName = "BOOSTHEAT.Boiler.GUI",
          }
        }
      );
      Check.That(manifest.Content.MCU).IsEqualTo(new []
        {
          new Manifest.PartData()
          {
            Revision = "2021.6.15.1",
            Target = "mcu_iu",
            FileName = "Carte_HAUT.bin",
          },
          new Manifest.PartData()
          {
            Revision = "2021.6.15.1",
            Target = "mcu_heat_pump",
            FileName = "Carte_BAS.bin",
          },
          new Manifest.PartData()
          {
            Revision = "2021.6.15.1",
            Target = "mcu_eu",
            FileName = "Carte_UE.bin",
          }
        }
      );
      Check.That(manifest.Content.BSP).IsEquivalentTo(new []
        {
          new Manifest.PartData()
          {
            Revision = "20210804.1",
            Target = "boiler_bsp",
            FileName = "update-bundle-colibri-imx7-emmc.raucb",
          }
        }
      );
    }

    [Test]
    public void serialize()
    {
      var manifest = new Manifest()
      {
        Device = "BOOSTHEAT.20_V2",
        SHA256 = "a7c0f2cafd266277c2987d0e88a0a384b402224548806aee9f5734627cec0407",
        Revision = "2.3.4.613",
        Date = new DateTime(2021,08,23,10,04,24),
        Content = new Manifest.ContentData()
        {
          APPS = new []
          {
            new Manifest.PartData()
            {
              Revision = "2021.8.23.1",
              Target = "boiler_app",
              FileName = "boiler_app.zip",
            },
            new Manifest.PartData()
            {
              Revision = "2021.8.6.1",
              Target = "boiler_gui",
              FileName = "BOOSTHEAT.Boiler.GUI",
            }
          },
          MCU = new []
          {
            new Manifest.PartData()
            {
              Revision = "2021.6.15.1",
              Target = "mcu_iu",
              FileName = "Carte_HAUT.bin",
            },
            new Manifest.PartData()
            {
              Revision = "2021.6.15.1",
              Target = "mcu_heat_pump",
              FileName = "Carte_BAS.bin",
            },
            new Manifest.PartData()
            {
              Revision = "2021.6.15.1",
              Target = "mcu_eu",
              FileName = "Carte_UE.bin",
            }
          },
          BSP = new []
          {
            new Manifest.PartData()
            {
              Revision = "20210804.1",
              Target = "boiler_bsp",
              FileName = "update-bundle-colibri-imx7-emmc.raucb",
            }
          }
        }
      };
      var manifestRead = Manifest.FromFile("package_examples/nominal_manifest.json");
      Check.That(manifestRead.IsSuccess).IsTrue();
      var expectedManifest = manifestRead.Value;
      
      Check.That(manifest.ToJsonText()).IsEqualTo(expectedManifest.ToJsonText());
    }
  }
}