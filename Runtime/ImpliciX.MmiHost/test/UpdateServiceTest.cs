using System;
using System.IO;
using System.Linq;
using ImpliciX.Data;
using ImpliciX.MmiHost.Services;
using NUnit.Framework;

namespace ImpliciX.MmiHost.Tests
{
    [TestFixture]
    [Platform(Include = "Unix")]
    public class UpdateServiceTest
    {
        private const string BootFsPath = "/tmp/Generated/slot/";
        private static readonly Func<string> GetOppositePartition = () => "bootfs.1";
        private static string SystemSoftwarePath = "/tmp/Generated/";
        private string Revision;

        public UpdateServiceTest()
        {
            Revision = "7.7.7.7";
        }

        private const string SelfContainedFileName = "BOOSTHEAT.Boiler.GUICOPY";
        private const string SelfContainedPackagePath = "package_examples/" + SelfContainedFileName;
        private const string ZippedFileName = "boiler_app_copy.zip";
        private const string ZippedPackagePath = "package_examples/" + ZippedFileName;
        private const string SeflContainedSoftwareName = "gui";
        private const string ZippedSoftwareName = "boiler_app";

        [SetUp]
        public static void Init()
        {
            var tmpGui = new DirectoryInfo("/tmp/Generated/gui");
            var tmpBoilerApp = new DirectoryInfo($"{SystemSoftwarePath}{ZippedSoftwareName}");
            var tmpSlotBootFs1 = new DirectoryInfo(Path.Combine(BootFsPath, GetOppositePartition()));
            tmpGui.Create();
            tmpBoilerApp.Create();
            tmpSlotBootFs1.Create();
            var boilerAppFile = new FileInfo("package_examples/boiler_app.zip");
            File.Copy(boilerAppFile.FullName, ZippedPackagePath, true);
            var guiFile = new FileInfo("package_examples/BOOSTHEAT.Boiler.GUI");
            guiFile.CopyTo(SelfContainedPackagePath, true);
        }

        [TearDown]
        public static void Clean()
        {
            var tmp = new DirectoryInfo(SystemSoftwarePath);
            if (tmp.Exists) tmp.Delete(true);
            var guiFile = new FileInfo(SelfContainedPackagePath);
            if (guiFile.Exists) guiFile.Delete();
            var boilerAppFile = new FileInfo(ZippedPackagePath);
            if (boilerAppFile.Exists) boilerAppFile.Delete();
        }

        [Test]
        public void update_app_packaged_as_zip_file_nominal_case()
        {
            var swUpdate =
                new SoftwareUpdate(ZippedSoftwareName, Revision, new FileInfo(ZippedPackagePath), SystemSoftwarePath, BootFsPath, GetOppositePartition());
            AppsUpdateService.LaunchUpdate(swUpdate);
            var targetFolder = new DirectoryInfo($"{SystemSoftwarePath}{ZippedSoftwareName}/{Revision}");
            var temporaryZip = new FileInfo($"{SystemSoftwarePath}{ZippedSoftwareName}/" + ZippedFileName);
            Assert.IsTrue(targetFolder.Exists);
            Assert.AreEqual(144, targetFolder.EnumerateFiles().Count());
            Assert.IsTrue(!temporaryZip.Exists);
        }

        [Test]
        public void update_app_packaged_as_self_contained_file_nominal_case()
        {
            var swUpdate =
                new SoftwareUpdate(SeflContainedSoftwareName, Revision, new FileInfo(SelfContainedPackagePath), SystemSoftwarePath, BootFsPath, GetOppositePartition());
            AppsUpdateService.LaunchUpdate(swUpdate);

            var targetFolder = new DirectoryInfo($"{SystemSoftwarePath}{SeflContainedSoftwareName}/{Revision}");
            var fileApp = new FileInfo(Path.Combine(targetFolder.FullName, SelfContainedFileName));
            Assert.IsTrue(targetFolder.Exists);
            Assert.IsTrue(fileApp.Exists);
        }

        [Test]
        public void update_app_packaged_as_zip_with_files_already_existing_on_target()
        {
            var targetFolder = new DirectoryInfo($"{SystemSoftwarePath}{ZippedSoftwareName}/{Revision}");
            targetFolder.Create();
            File.CreateText(Path.Combine(targetFolder.FullName, "test.txt"));

            var swUpdate =
                new SoftwareUpdate(ZippedSoftwareName, Revision, new FileInfo(ZippedPackagePath), SystemSoftwarePath, BootFsPath, GetOppositePartition());
            AppsUpdateService.LaunchUpdate(swUpdate);

            var temporaryZip = new FileInfo($"{SystemSoftwarePath}{ZippedSoftwareName}/" + ZippedFileName);
            Assert.IsTrue(targetFolder.Exists);
            Assert.AreEqual(144, targetFolder.EnumerateFiles().Count());
            Assert.IsTrue(!temporaryZip.Exists);
        }

        [Test]
        public void update_app_packaged_as_self_contained_with_files_already_exist_on_target()
        {
            var targetFolder = new DirectoryInfo($"{SystemSoftwarePath}{SeflContainedSoftwareName}/{Revision}");
            targetFolder.Create();
            File.CreateText(Path.Combine(targetFolder.FullName, SelfContainedFileName));

            var swUpdate =
                new SoftwareUpdate(SeflContainedSoftwareName, Revision, new FileInfo(SelfContainedPackagePath), SystemSoftwarePath, BootFsPath, GetOppositePartition());
            AppsUpdateService.LaunchUpdate(swUpdate);

            var fileApp = new FileInfo(Path.Combine(targetFolder.FullName, SelfContainedFileName));
            Assert.IsTrue(targetFolder.Exists);
            Assert.IsTrue(fileApp.Exists);
            Assert.IsTrue(fileApp.Length > 0);
        }

        //
        [Test]
        public void should_update_self_contained_app_symlink()
        {
            var swUpdate =
                new SoftwareUpdate(SeflContainedSoftwareName, Revision, new FileInfo(SelfContainedPackagePath), SystemSoftwarePath, BootFsPath, GetOppositePartition());
            AppsUpdateService.LaunchUpdate(swUpdate);
            var symlink = new DirectoryInfo(Path.Combine(BootFsPath, GetOppositePartition(), SeflContainedSoftwareName));
            Assert.IsTrue(symlink.Exists);
            Assert.AreEqual(symlink.EnumerateFiles().Single().Name, SelfContainedFileName);
        }

        [Test]
        public void should_zip_packaged_app_update_symlink()
        {
            var swUpdate =
                new SoftwareUpdate(ZippedSoftwareName, Revision, new FileInfo(ZippedPackagePath), SystemSoftwarePath, BootFsPath, GetOppositePartition());
            AppsUpdateService.LaunchUpdate(swUpdate);

            var symlink = new DirectoryInfo(Path.Combine(BootFsPath, GetOppositePartition(), ZippedSoftwareName));
            Assert.IsTrue(symlink.Exists);
            Assert.AreEqual(symlink.EnumerateFiles().Count(), 144);
        }

        [Test]
        public void should_update_symlink_when_symlink_already_exist()
        {
            CreateFakeBootFsAndAppFolder();
            Fs.TryCreateSymbolicLink($"{SystemSoftwarePath}{SeflContainedSoftwareName}/{Revision}", $"{BootFsPath}bootfs.1/{SeflContainedSoftwareName}");

            var swUpdate =
                new SoftwareUpdate(SeflContainedSoftwareName, Revision, new FileInfo(SelfContainedPackagePath), SystemSoftwarePath, BootFsPath, GetOppositePartition());
            AppsUpdateService.LaunchUpdate(swUpdate);

            var symlinkPath = new DirectoryInfo(Path.Combine(BootFsPath, GetOppositePartition(), SeflContainedSoftwareName));
            Assert.IsTrue(symlinkPath.Exists);
        }

        void CreateFakeBootFsAndAppFolder()
        {
            new DirectoryInfo($"{SystemSoftwarePath}{SeflContainedSoftwareName}/{Revision}").Create();
            new DirectoryInfo(Path.Combine(BootFsPath, GetOppositePartition())).Create();
            File.CreateText(Path.Combine($"{SystemSoftwarePath}{SeflContainedSoftwareName}/{Revision}", SelfContainedFileName));
        }
    }
}