using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using ImpliciX.Designer.ViewModels.LiveMenu;
using ImpliciX.DesktopServices;
using Moq;
using NFluent;
using NUnit.Framework;
using static System.IO.Path;

namespace ImpliciX.Designer.Tests.ViewModels;

public class LiveBackupSystemHistoryTests
{
  [Test]
  public void IsDisabledByDefault()
  {
    Check.That(_sut.IsEnabled).IsFalse();
  }

  [TestCase(false, false, false)]
  [TestCase(true, false, true)]
  [TestCase(false, true, true)]
  [TestCase(true, true, true)]
  public void IsEnabledOrDisabledDependingOnCapabilities(bool hasJournal, bool hasHistory, bool expected)
  {
    SetTargetSystem(hasJournal, hasHistory);
    Check.That(_sut.IsEnabled).IsEqualTo(expected);
  }

  [Test]
  public void NoBackupWhenUserDoesNotSelectFolder()
  {
    _user.Setup(x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Cancel, ""))).Verifiable();
    SetTargetSystem(true, true);
    _sut.Open();
    _user.Verify();
    Check.That(_consoleOutput).IsEmpty();
  }

  [Test]
  public void BackupJournalWhenUserSelectsFolder()
  {
    SetTargetSystem(true, false);
    UserSelectsFolder("the_folder");
    _sut.Open();
    _user.Verify();
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting backup of system history",
      "Journal log.txt.gz into the_folder",
      "Backup of system history complete",
    });
    _journalBackupExecution.Verify(x => x.AndSaveTo($"the_folder{DirectorySeparatorChar}log.txt.gz"));
  }

  [Test]
  public void BackupHistoryWhenUserSelectsFolder()
  {
    UserSelectsFolder("the_folder");
    SetTargetSystem(false, true);
    _historyBackup.Setup(x => x.Execute().AndSaveManyTo("the_folder"))
      .Returns(AsyncEnumerable(new[]
      {
        (1, 3, "foo", ""),
        (2, 3, "bar", ""),
        (3, 3, "qix", ""),
      }));
    _sut.Open();
    _user.Verify();
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting backup of system history",
      "[1/3] foo into the_folder",
      "[2/3] bar into the_folder",
      "[3/3] qix into the_folder",
      "Backup of system history complete",
    });
  }

  [Test]
  public void BackupJournalAndHistoryWhenUserSelectsFolder()
  {
    UserSelectsFolder("another_folder");
    SetTargetSystem(true, true);
    _historyBackup.Setup(x => x.Execute().AndSaveManyTo("another_folder"))
      .Returns(AsyncEnumerable(new[]
      {
        (1, 2, "foo", ""),
        (2, 2, "bar", ""),
      }));
    _sut.Open();
    _user.Verify();
    Check.That(_consoleOutput).IsEqualTo(new[]
    {
      "Starting backup of system history",
      "Journal log.txt.gz into another_folder",
      "[1/2] foo into another_folder",
      "[2/2] bar into another_folder",
      "Backup of system history complete",
    });
    _journalBackupExecution.Verify(x => x.AndSaveTo($"another_folder{DirectorySeparatorChar}log.txt.gz"));
  }
#pragma warning disable CS1998
  private static async IAsyncEnumerable<T> AsyncEnumerable<T>(IEnumerable<T> xs)
  {
    foreach (var x in xs) yield return x;
  }

  private void UserSelectsFolder(string folderName)
  {
    _user.Setup(x => x.OpenFolder(It.IsAny<IUser.FileSelection>()))
      .Returns(Task.FromResult((IUser.ChoiceType.Ok, folderName))).Verifiable();
  }

  private void SetTargetSystem(bool hasJournal, bool hasHistory)
  {
    _journalBackup = new Mock<ITargetSystemCapability>();
    _historyBackup = new Mock<ITargetSystemCapability>();
    _targetSystem = new Mock<ITargetSystem>();
    _targetSystem.Setup(x => x.SystemJournalBackup).Returns(_journalBackup.Object);
    _targetSystem.Setup(x => x.SystemHistoryBackup).Returns(_historyBackup.Object);
    _journalBackup.Setup(x => x.IsAvailable).Returns(hasJournal);
    _historyBackup.Setup(x => x.IsAvailable).Returns(hasHistory);
    _journalBackupExecution = new Mock<ITargetSystemCapability.IExecution>();
    _journalBackup.Setup(x => x.Execute()).Returns(_journalBackupExecution.Object);
    _targetSystems.OnNext(_targetSystem.Object);
  }

  private LiveBackupSystemHistory _sut;
  private Subject<ITargetSystem> _targetSystems;
  private List<string> _consoleOutput;
  private Mock<IUser> _user;
  private Mock<ITargetSystemCapability> _journalBackup;
  private Mock<ITargetSystemCapability.IExecution> _journalBackupExecution;
  private Mock<ITargetSystemCapability> _historyBackup;
  private Mock<ITargetSystem> _targetSystem;

  [SetUp]
  public void Init()
  {
    var concierge = new Mock<ILightConcierge>();
    var console = new Mock<IConsoleService>();
    _consoleOutput = new List<string>();
    console.Setup(x => x.WriteLine(It.IsAny<string>()))
      .Callback<string>(t => _consoleOutput.Add(t));
    concierge.Setup(x => x.Console).Returns(console.Object);
    _user = new Mock<IUser>();
    concierge.Setup(x => x.User).Returns(_user.Object);
    var remote = new Mock<IRemoteDevice>();
    concierge.Setup(x => x.RemoteDevice).Returns(remote.Object);
    _targetSystems = new Subject<ITargetSystem>();
    remote.Setup(x => x.TargetSystem).Returns(_targetSystems);
    _sut = new LiveBackupSystemHistory(concierge.Object);
  }
}
