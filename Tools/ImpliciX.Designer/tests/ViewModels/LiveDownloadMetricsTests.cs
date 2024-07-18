using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ImpliciX.Data;
using ImpliciX.Designer.ViewModels.LiveMenu;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;
using Moq;
using NFluent;
using NUnit.Framework;
using static System.Environment;
using static System.IO.Path;
using MetricsExport = System.Func<
  ImpliciX.DesktopServices.ILightConcierge,
  System.Func<string, string>,
  System.Func<string, string, ImpliciX.Language.Core.Result<ImpliciX.Language.Core.Unit>>,
  System.Func<System.DateTime>,
  ImpliciX.Designer.ViewModels.LiveMenu.LiveDownloadColdData
>;
  
namespace ImpliciX.Designer.Tests.ViewModels;

[TestFixtureSource(nameof(ExportCases))]
public class LiveDownloadMetricsTests
{
  static object[] ExportCases =
  {
    new object[]
    {
      LiveDownloadColdData.XlFileExtension,
      LiveDownloadColdData.XlMetrics,
      (LiveDownloadMetricsTests t) =>
      {
        t._concierge.Verify(
          o =>
            o.Export.MetricsToExcel("the_folder", It.IsAny<string>())
          );
      }
    },
    new object[]
    {
      LiveDownloadColdData.SqlFileExtension, 
      LiveDownloadColdData.SqlMetrics,
      (LiveDownloadMetricsTests t) =>
      {
        t._concierge.Verify(
          o =>
            o.Export.MetricsToSqlite("the_folder", It.IsAny<string>())
        );
      }
    }
  };

  public LiveDownloadMetricsTests(string extension, MetricsExport export, Action<LiveDownloadMetricsTests> verifications)
  {
    _export = export;
    _verifications = verifications;
    _extension = extension;
  }

  private readonly MetricsExport _export;
  private readonly Action<LiveDownloadMetricsTests> _verifications;
  private readonly string _extension;

  private static readonly string StorageFolderPath = Combine(GetTempPath(), nameof(LiveDownloadMetricsTests));
  private static readonly string MetricsColdPath = Combine("Resources", "metricsCold");

  private const string CurrentDate = "2023-08-31";

  private List<string> _consoleOutput;
  private Mock<IUser> _user;
  private Subject<ITargetSystem> _targetSystems;
  private Mock<ITargetSystem> _targetSystem;
  private Mock<ITargetSystemCapability> _coldDownload;
  private Mock<ITargetSystemCapability> _coldClear;
  private Mock<ITargetSystemCapability.IExecution> _coldDownloadExecution;
  private Mock<ITargetSystemCapability.IExecution> _coldClearExecution;
  private Mock<IFileSystemService> _fileService;
  private Mock<ILightConcierge> _concierge;

  [TearDown]
  public void TearDown()
  {
    if (Directory.Exists(StorageFolderPath))
      Directory.Delete(StorageFolderPath, true);
  }

  [Test]
  public void IsDisabledByDefault()
  {
    Check.That(CreatedSut().IsEnabled).IsFalse();
  }


  [Test]
  public void NoDownloadWhenUserDoesNotSelectFolder()
  {
    _user.Setup(x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Cancel, ""))).Verifiable();

    var sut = CreatedSut();
    SetTargetSystem(true, true);
    sut.OpenAsync().Wait();
    _user.Verify();
    Check.That(_consoleOutput).ContainsExactly("Output folder selection: Canceled by user");

    _coldDownload.Verify(o => o.Execute(), Times.Never);
    _coldClear.Verify(o => o.Execute(), Times.Never);
  }

  [Test]
  public void GivenNominalCaseWithRealMetricsColdStorageFile_WhenDownloadMetrics_ThenOutputFileIsExported()
  {
    // Given
    var workingDirectory = CreateTempDir();
    var sourceDir = Combine(workingDirectory, "finished");
    Directory.CreateDirectory(sourceDir);

    const string sourceFileName = "1.metrics.zip";
    var sourceFilePath = Combine(sourceDir, sourceFileName);
    File.Copy(Combine(MetricsColdPath, sourceFileName), sourceFilePath);

    var sut = CreatedSut(Zip.ExtractToDirectory);
    _coldDownload.Setup(x => x.Execute().AndSaveManyTo(sourceDir))
      .Returns(AsyncEnumerable(new[]
      {
        (1, 1, sourceFileName, ComputeFileChecksum(sourceFileName))
      }));

    _fileService.Setup(o => o.DirectoryGetFiles(It.IsAny<string>(), It.IsAny<string>()))
      .Returns<string, string>(Directory.GetFiles);

    UserSelectsFolder(sourceDir);
    sut.OpenAsync().Wait();
    _user.Verify();
    var outputFilePathExpected = Combine(sourceDir, $"{CurrentDate}_metrics{_extension}");
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting download data",
      $"[1/1] {sourceFileName} into {sourceDir}",
      "Download of 1 files completed",
      "Unzip files download in progress",
      $"[1/1] {sourceFileName}: unzipped & removed successfully",
      "Unzip files download completed",
      $"Export to {outputFilePathExpected} in progress",
      $"Export to {outputFilePathExpected} completed",
      "Clear remote cold storage in progress",
      "Clear remote cold storage completed",
    });

    _coldDownload.Verify(o => o.Execute());
    _coldClear.Verify(o => o.Execute());
    _fileService.Verify(o => o.DeleteFile(sourceFilePath));
  }

  [Test]
  public void GivenNominalCase_WhenDownloadMetrics_ThenOutputFileIsExported()
  {
    var extractedFiles = new List<string>();
    var sut = CreatedSut((file, _) =>
    {
      extractedFiles.Add(file);
      return default(Unit);
    });
    _coldDownload.Setup(x => x.Execute().AndSaveManyTo("the_folder"))
      .Returns(AsyncEnumerable(new[]
      {
        (1, 3, "foo", ComputeFileChecksum("foo")),
        (2, 3, "bar", ComputeFileChecksum("bar")),
        (3, 3, "qix", ComputeFileChecksum("qix"))
      }));

    _fileService.Setup(o => o.DirectoryGetFiles("the_folder", It.IsAny<string>()))
      .Returns(new[] { "foo", "bar", "qix" });

    UserSelectsFolder("the_folder");

    sut.OpenAsync().Wait();

    _user.Verify();
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting download data",
      "[1/3] foo into the_folder",
      "[2/3] bar into the_folder",
      "[3/3] qix into the_folder",
      "Download of 3 files completed",
      "Unzip files download in progress",
      "[1/3] foo: unzipped & removed successfully",
      "[2/3] bar: unzipped & removed successfully",
      "[3/3] qix: unzipped & removed successfully",
      "Unzip files download completed",
      $"Export to the_folder{DirectorySeparatorChar}{CurrentDate}_metrics{_extension} in progress",
      $"Export to the_folder{DirectorySeparatorChar}{CurrentDate}_metrics{_extension} completed",
      "Clear remote cold storage in progress",
      "Clear remote cold storage completed",
    });

    _coldDownload.Verify(o => o.Execute());
    _coldClear.Verify(o => o.Execute());
    _verifications(this);
    _fileService.Verify(o => o.DeleteFile("foo"));
    _fileService.Verify(o => o.DeleteFile("bar"));
    _fileService.Verify(o => o.DeleteFile("qix"));
    Check.That(extractedFiles).ContainsExactly("foo", "bar", "qix");
  }

  [Test]
  public void GivenRemoveLocalColdStorageDownloadedIsFalse_WhenDownloadMetrics_ThenOutputFileIsExported()
  {
    _concierge.Setup(o => o.RuntimeFlags)
      .Returns(new RuntimeFlags(envVarName =>
        envVarName == RuntimeFlags.RemoveLocalColdStorageDownloaded_EnvVarName
          ? "Something"
          : null)
      );

    var extractedFileRequests = new List<string>();
    var sut = CreatedSut((file, _) =>
    {
      extractedFileRequests.Add(file);
      return default(Unit);
    });

    _coldDownload.Setup(x => x.Execute().AndSaveManyTo("the_folder"))
      .Returns(AsyncEnumerable(new[]
      {
        (1, 3, "foo", ComputeFileChecksum("foo")),
        (2, 3, "bar", ComputeFileChecksum("bar")),
        (3, 3, "qix", ComputeFileChecksum("qix"))
      }));

    _fileService.Setup(o => o.DirectoryGetFiles("the_folder", It.IsAny<string>()))
      .Returns(new[] { "foo", "bar", "qix" });

    UserSelectsFolder("the_folder");
    sut.OpenAsync().Wait();

    _user.Verify();
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting download data",
      "[1/3] foo into the_folder",
      "[2/3] bar into the_folder",
      "[3/3] qix into the_folder",
      "Download of 3 files completed",
      "Unzip files download in progress",
      "[1/3] foo: unzipped successfully",
      "[2/3] bar: unzipped successfully",
      "[3/3] qix: unzipped successfully",
      "Unzip files download completed",
      $"Export to the_folder{DirectorySeparatorChar}{CurrentDate}_metrics{_extension} in progress",
      $"Export to the_folder{DirectorySeparatorChar}{CurrentDate}_metrics{_extension} completed",
      "Clear remote cold storage in progress",
      "Clear remote cold storage completed",
    });

    _coldDownload.Verify(o => o.Execute());
    _coldClear.Verify(o => o.Execute());
    _verifications(this);
    _fileService.Verify(o => o.DeleteFile(It.IsAny<string>()), Times.Never);
    Check.That(extractedFileRequests).ContainsExactly("foo", "bar", "qix");
  }

  [Test]
  public void GivenOneFileUnzipFailed_WhenDownloadMetrics_ThenIExportThatICanAndIKeepFileUnzipFailed()
  {
    var extractedFileRequests = new List<string>();
    var sut = CreatedSut((file, _) =>
    {
      extractedFileRequests.Add(file);
      return file == "bar"
        ? Result<Unit>.Create(new Error("Tests", "Error when unzip bar"))
        : default(Unit);
    });
    _coldDownload.Setup(x => x.Execute().AndSaveManyTo("the_folder"))
      .Returns(AsyncEnumerable(new[]
      {
        (1, 3, "foo", ComputeFileChecksum("foo")),
        (2, 3, "bar", ComputeFileChecksum("bar")),
        (3, 3, "qix", ComputeFileChecksum("qix"))
      }));

    _fileService.Setup(o => o.DirectoryGetFiles("the_folder", It.IsAny<string>()))
      .Returns(new[] { "foo", "bar", "qix" });

    UserSelectsFolder("the_folder");

    sut.OpenAsync().Wait();

    _user.Verify();
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting download data",
      "[1/3] foo into the_folder",
      "[2/3] bar into the_folder",
      "[3/3] qix into the_folder",
      "Download of 3 files completed",
      "Unzip files download in progress",
      "[1/3] foo: unzipped & removed successfully",
      "[2/3] bar: ERROR Tests:Error when unzip bar",
      "[3/3] qix: unzipped & removed successfully",
      $"Export to the_folder{DirectorySeparatorChar}{CurrentDate}_metrics{_extension} in progress",
      $"Export to the_folder{DirectorySeparatorChar}{CurrentDate}_metrics{_extension} completed",
      "Clear remote cold storage in progress",
      "Clear remote cold storage completed",
      "Unzip: ERROR: Some files failed"
    });

    _coldDownload.Verify(o => o.Execute());
    _coldClear.Verify(o => o.Execute());
    _verifications(this);
    _fileService.Verify(o => o.DeleteFile("foo"));
    _fileService.Verify(o => o.DeleteFile("bar"), Times.Never);
    _fileService.Verify(o => o.DeleteFile("qix"));
    Check.That(extractedFileRequests).ContainsExactly("foo", "bar", "qix");
  }

  [Test]
  public void GivenOneFileHasChecksumError_WhenDownloadMetrics_ThenIDoNotClearColdStorage()
  {
    var sut = CreatedSut();
    var barLocalChecksum = ComputeFileChecksum("bar");
    var barRemoteChecksum = ComputeFileChecksum("barOnRemote");
    _coldDownload.Setup(x => x.Execute().AndSaveManyTo("the_folder"))
      .Returns(AsyncEnumerable(new[]
      {
        (1, 3, "foo", ComputeFileChecksum("foo")),
        (2, 3, "bar", barRemoteChecksum),
        (3, 3, "qix", ComputeFileChecksum("qix")),
      }));

    UserSelectsFolder("the_folder");
    sut.OpenAsync().Wait();
    _user.Verify();
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting download data",
      "[1/3] foo into the_folder",
      "[2/3] bar into the_folder",
      "[3/3] qix into the_folder",
      $"Integrity check has failed for:{NewLine}the_folder{DirectorySeparatorChar}bar : expected={barRemoteChecksum}, computed={barLocalChecksum}"
    });

    _coldDownload.Verify(o => o.Execute());
    _coldClear.Verify(o => o.Execute(), Times.Never);
  }

  [Test]
  public void GivenRemoteColdFolderIsEmpty_WhenIDownload_ThenIGetAnErrorMessage()
  {
    var sut = CreatedSut();
    UserSelectsFolder("the_folder");
    _coldDownload.Setup(x => x.Execute().AndSaveManyTo("the_folder"))
      .Returns(AsyncEnumerable(Array.Empty<(int, int, string, string)>()));

    sut.OpenAsync().Wait();
    _user.Verify();
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting download data",
      "Download: No file downloaded"
    });

    _coldDownload.Verify(o => o.Execute());
    _coldClear.Verify(o => o.Execute(), Times.Never);
  }

  private void SetTargetSystem(bool hasColdStorage, bool hasClear)
  {
  }

  [SetUp]
  public void Init()
  {
    _concierge = new Mock<ILightConcierge>();
    var console = new Mock<IConsoleService>();
    var remote = new Mock<IRemoteDevice>();
    _user = new Mock<IUser>();
    _consoleOutput = new List<string>();
    _targetSystems = new Subject<ITargetSystem>();
    _fileService = new Mock<IFileSystemService>();

    console.Setup(x => x.WriteLine(It.IsAny<string>())).Callback<string>(t => _consoleOutput.Add(t));
    _concierge.Setup(x => x.Console).Returns(console.Object);
    _concierge.Setup(x => x.User).Returns(_user.Object);
    _concierge.Setup(x => x.RemoteDevice).Returns(remote.Object);
    _concierge.Setup(o => o.RuntimeFlags).Returns(new RuntimeFlags(_ => null));
    _concierge.Setup(o => o.FileSystemService).Returns(_fileService.Object);
    _concierge.Setup(o => o.Export.MetricsToExcel(It.IsAny<string>(), It.IsAny<string>()))
      .Returns(new Func<string, string, FakeExporter>((f, o) => new FakeExporter(console.Object, f, o)));
    _concierge.Setup(o => o.Export.MetricsToSqlite(It.IsAny<string>(), It.IsAny<string>()))
      .Returns(new Func<string, string, FakeExporter>((f, o) => new FakeExporter(console.Object, f, o)));
    _coldDownload = new Mock<ITargetSystemCapability>();
    _coldDownload.Setup(x => x.IsAvailable).Returns(true);
    _coldDownloadExecution = new Mock<ITargetSystemCapability.IExecution>();
    _coldDownload.Setup(x => x.Execute()).Returns(_coldDownloadExecution.Object);

    _coldClear = new Mock<ITargetSystemCapability>();
    _coldClear.Setup(x => x.IsAvailable).Returns(true);
    _coldClearExecution = new Mock<ITargetSystemCapability.IExecution>();
    _coldClear.Setup(x => x.Execute()).Returns(_coldClearExecution.Object);

    _targetSystem = new Mock<ITargetSystem>();
    _targetSystem.Setup(x => x.MetricsColdStorageDownload).Returns(_coldDownload.Object);
    _targetSystem.Setup(x => x.MetricsColdStorageClear).Returns(_coldClear.Object);
    _targetSystems.OnNext(_targetSystem.Object);
    remote.Setup(x => x.TargetSystem).Returns(_targetSystems);
    remote.Setup(x => x.CurrentTargetSystem).Returns(_targetSystem.Object);
  }

  private LiveDownloadColdData CreatedSut(Func<string, string, Result<Unit>> unzipToDirectory = null)
    => _export(_concierge.Object, ComputeFileChecksum, unzipToDirectory ?? ((_, _) => default(Unit)),
      () => DateTime.Parse(CurrentDate));

  private static string ComputeFileChecksum(string filePath)
  {
    var fileName = GetFileName(filePath);
    return $"FakeSHA256_{fileName}";
  }

  private void UserSelectsFolder(string folderName)
  {
    _user.Setup(x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Ok, folderName))).Verifiable();
  }

  private static string CreateTempDir()
  {
    var tmpDirPath = Combine(StorageFolderPath, GetFileNameWithoutExtension(GetTempFileName()));
    Directory.CreateDirectory(tmpDirPath);
    return tmpDirPath;
  }

#pragma warning disable CS1998
  private static async IAsyncEnumerable<T> AsyncEnumerable<T>(IEnumerable<T> xs)
  {
    foreach (var x in xs) yield return x;
  }
}

public class FakeExporter : IAction
{
  private readonly IConsoleService _console;
  private readonly string _folder;
  private readonly string _output;

  public FakeExporter(IConsoleService console, string folder, string output)
  {
    _console = console;
    _folder = folder;
    _output = output;
  }

  public Result<Unit> Execute()
  {
    return default(Unit);
  }
}