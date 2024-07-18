using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Unix.Native;
using NFluent;
using NUnit.Framework;
using static Mono.Unix.Native.FilePermissions;

namespace ImpliciX.Data.Tests
{
    
    public class ZipTests
    {
        private readonly FileInfo TmpFolder = new("./tmp");
        private readonly FileInfo ZipFileWithSubDir = new("./test_artefact/sample.zip");
        private readonly FileInfo ZipFileWithSingleFile = new("./test_artefact/single_file_sample.zip");

        [Test]
        [Platform(Include = "Unix,Win")]
        public void should_decompress_files_found_the_root_level_of_the_archive()
        {
            Zip.ExtractToDirectory(ZipFileWithSingleFile.FullName, TmpFolder.FullName);
            var expectedFileAtRoot = Path.Combine(TmpFolder.FullName, "file.txt");
            Check.That(File.Exists(expectedFileAtRoot)).IsTrue();
        }

        [Test]
        [Platform(Include = "Unix,Win")]
        public void should_decompress_files_found_the_sub_dir_of_the_archive()
        {
            Zip.ExtractToDirectory(ZipFileWithSubDir.FullName, TmpFolder.FullName);
            var expectedFileAtRoot = Path.Combine(TmpFolder.FullName, "file.txt");
            var expectedFileInSubDir = Path.Combine(TmpFolder.FullName, "subdir", "script.sh");
            Check.That(File.Exists(expectedFileAtRoot)).IsTrue();
            Check.That(File.Exists(expectedFileInSubDir)).IsTrue();
        }


        [Test]
        [Platform(Include = "Unix")]
        public void should_restore_file_flags_decompress()
        {
            Zip.ExtractToDirectory(ZipFileWithSubDir.FullName, TmpFolder.FullName);
            var expectedFileInSubDir = Path.Combine(TmpFolder.FullName, "subdir", "script.sh");
            var fd = Syscall.open(expectedFileInSubDir, OpenFlags.O_RDONLY);
            Syscall.fstat(fd, out var stat);
            Check.That(stat.st_mode).IsEqualTo(S_IXOTH | S_IROTH | S_IRWXG | S_IRWXU | S_IFREG);
        }

        [Test]
        [Platform(Include = "Unix, Win")]
        public void should_override_files_in_destination_folder_if_exists()
        {
            Zip.ExtractToDirectory(ZipFileWithSingleFile.FullName, TmpFolder.FullName);
            Zip.ExtractToDirectory(ZipFileWithSingleFile.FullName, TmpFolder.FullName);
            Assert.Pass();
        }

        [Test]
        [Platform(Include = "Unix,Win")]
        public void should_create_zip_nominal_case()
        {
            var files = new[] { "example.sh", "file.txt" };
            var zipFile = Path.Combine(TmpFolder.FullName, "dummy.zip");

            var filesToZip = files.Select(s => Path.Combine("test_artefact", s)).ToArray();

            var result = Zip.CreateZipFromFiles(filesToZip, zipFile);
            Assert.IsTrue(result.IsSuccess, result.Error?.Message);

            Zip.ExtractToDirectory(zipFile, TmpFolder.FullName);
            var actualFiles = files.Select(s => Path.Combine(TmpFolder.FullName, s)).ToArray();

            foreach (var (actualFile, expectedFile) in actualFiles.Zip(filesToZip))
            {
                Check.That(Path.GetFileName(actualFile)).IsEqualTo(Path.GetFileName(expectedFile));
                Check.That(File.Exists(actualFile)).IsTrue();
                Check.That(HaveSamePermissionsOnLinux(actualFile, expectedFile)).IsTrue();
            }
        }

        [Test]
        [Platform(Include = "Unix,Win")]
        public void should_create_zip_error_case()
        {
            var result = Zip.CreateZipFromFiles(new[] { "file_that_not_exist.txt" }, "boom.zip");
            Assert.IsTrue(result.IsError);
        }

        [Test]
        [Platform(Include = "Unix,Win")]
        public void should_create_zip_from_directory()
        {
            var srcPath = Path.Combine("test_artefact", "SubFolder");
            var outputZipFile = Path.Combine(TmpFolder.FullName, "dummy2.zip");
            var extractTmpPath = Path.Combine(TmpFolder.FullName, "extractTmp");
            var expectedFiles = Directory.GetFiles(srcPath, "*", SearchOption.AllDirectories);

            var result = Zip.CreateZip(new[] { (srcPath, "toto") }, outputZipFile);
            Assert.IsTrue(result.IsSuccess, result.Error?.Message);

            Zip.ExtractToDirectory(outputZipFile, extractTmpPath);
            var actualFiles = Directory.GetFiles(extractTmpPath, "*", SearchOption.AllDirectories);

            Check.That(actualFiles.Length).IsEqualTo(expectedFiles.Length);
            foreach (var (actualFile, expectedFile) in actualFiles.Zip(expectedFiles))
            {
                Check.That(Path.GetFileName(actualFile)).IsEqualTo(Path.GetFileName(expectedFile));
                Check.That(File.Exists(actualFile)).IsTrue();
                Check.That(HaveSamePermissionsOnLinux(actualFile, expectedFile)).IsTrue();
            }
        }

        [Test]
        [Platform(Include = "Unix,Win")]
        public void should_create_zip_with_multiple_directories_and_files()
        {
            var srcPath = Path.Combine("test_artefact", "SubFolder");
            var outputZipFile = Path.Combine(TmpFolder.FullName, "dummy2.zip");
            var extractTmpPath = Path.Combine(TmpFolder.FullName, "extractTmp");

            var expectedFiles = new List<string>()
            {
                Path.Combine(extractTmpPath, "example.sh"),
                Path.Combine(extractTmpPath, "file.txt"),
                Path.Combine(extractTmpPath, "tata"),
                Path.Combine(extractTmpPath, "SubFolder2", "file2.txt")
            }.ToArray();

            var result = Zip.CreateZip(new[] { (srcPath, "toto"), (ZipFileWithSingleFile.FullName, "tata") }, outputZipFile);
            Assert.IsTrue(result.IsSuccess, result.Error?.Message);

            Zip.ExtractToDirectory(outputZipFile, extractTmpPath);
            var actualFiles = Directory.GetFiles(extractTmpPath, "*", SearchOption.AllDirectories);

            Check.That(actualFiles.Length).IsEqualTo(expectedFiles.Length);
            foreach (var (actualFile, expectedFile) in actualFiles.Order().Zip(expectedFiles.Order()))
            {
                Check.That(Path.GetFileName(actualFile)).IsEqualTo(Path.GetFileName(expectedFile));
                Check.That(File.Exists(actualFile)).IsTrue();
                Check.That(HaveSamePermissionsOnLinux(actualFile, expectedFile)).IsTrue();
            }
        }

        private static bool HaveSamePermissionsOnLinux(string fileA, string fileB)
        {
            if(Environment.OSVersion.Platform != PlatformID.Unix)
                return true;
            var fdA = Syscall.open(fileA, OpenFlags.O_RDONLY);
            Syscall.fstat(fdA, out var statA);

            var fdB = Syscall.open(fileB, OpenFlags.O_RDONLY);
            Syscall.fstat(fdB, out var statB);

            return (int)statA.st_mode == (int)statB.st_mode;
        }


        [SetUp]
        public void Setup()
        {
            if (!Directory.Exists(TmpFolder.FullName))
                Directory.CreateDirectory(TmpFolder.FullName);
        }

        [TearDown]
        public void Teardown()
        {
            if (Directory.Exists(TmpFolder.FullName))
                Directory.Delete(TmpFolder.FullName, true);
        }
    }
}