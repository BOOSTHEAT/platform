using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Data.Tests
{
    [Platform(Include = "Unix")]
    [Category("ExcludeWindows")]
    public class HarmonyPackageLoaderTests
    {
        [Test]
        public void make_temp_copy_of_the_package()
        {
            var originalFilePath = "package_examples/nominal_case.zip";
            var packageLocation = PackageLocation.FromString($"file://{originalFilePath}").Value;
            var tempCopy = PackageLoader.MakeLocalTempCopy(packageLocation).GetValueOrDefault();
            var original = new FileInfo(originalFilePath);
            Check.That(tempCopy.Exists).IsTrue();
            Check.That(tempCopy.Length).IsEqualTo(original.Length);
        }

        [Test]
        public void harmony_package_loading_nominal_test()
        {
            var originalFilePath = "package_examples/nominal_case.zip";
            var packageLocation = PackageLocation.FromString($"file://{originalFilePath}").Value;
            var loadResult = PackageLoader.Load(
                packageLocation,
                k => new Dictionary<string, SoftwareDeviceNode>
                {
                    ["mcu_eu"] = dummy.mcu1,
                    ["mcu_iu"] = dummy.mcu2,
                    ["mcu_heat_pump"] = dummy.mcu3,
                    ["boiler_app"] = dummy.app1,
                    ["boiler_gui"] = dummy.bsp
                }[k]);
            loadResult.CheckIsSuccessAnd(harmonyPackage =>
            {
                Check.That(harmonyPackage.ApplicationName).IsEqualTo("BOOSTHEAT.20_V2");
                var iu = harmonyPackage[dummy.mcu2].GetValue();
                Check.That(iu.Bytes.Take(5).ToArray()).IsEqualTo(new byte[] {72, 14, 0, 32, 157});
                var eu = harmonyPackage[dummy.mcu1].GetValue();
                Check.That(eu.Bytes.Take(5).ToArray()).IsEqualTo(new byte[] {24, 11, 0, 32, 53});
                var heatPump = harmonyPackage[dummy.mcu3].GetValue();
                Check.That(heatPump.Bytes.Take(5).ToArray()).IsEqualTo(new byte[] {152, 18, 0, 32, 157});
                var boiler_app = harmonyPackage[dummy.app1].GetValue();
                Check.That(boiler_app.Bytes.Take(5).ToArray()).IsEqualTo(new byte[] {80, 75, 3, 4, 20});
                var boiler_gui = harmonyPackage[dummy.bsp].GetValue();
                Check.That(boiler_gui.Bytes.Take(5).ToArray()).IsEqualTo(new byte[] {127, 69, 76, 70, 1});
            });
        }

        [Test]
        public void error_is_returned_when_loading_corrupted_package()
        {
            var originalFilePath = "package_examples/corrupted_package.zip";
            var packageLocation = PackageLocation.FromString($"file://{originalFilePath}").Value;
            var manifestLocation = "/tmp/package_examples/manifest.json";
            var loadResult = PackageLoader.Load(packageLocation, _ => null);
            loadResult.CheckIsErrorAnd(error =>
            {
                Check.That(error is UpdateError).IsTrue();
                Check.That(new FileInfo(manifestLocation).Exists).IsFalse();
            });
        }
        
        
        [TearDown]
        public void Clean()
        {
            var tmpPackageDirectory = new DirectoryInfo("/tmp/package_examples/");
            if(tmpPackageDirectory.Exists) tmpPackageDirectory.Delete(true);
        }
    }
}