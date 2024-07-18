using ImpliciX.DesktopServices.Services;
using Moq;

namespace ImpliciX.DesktopServices.Tests;

[TestFixture]
public class FileLoggerTests
{
  private string _tempPath;
  private string _appName;
  private string _filePrefix;

  [SetUp]
  public void SetUp()
  {
    _tempPath = Path.Combine(Path.GetTempPath(), "FileLoggerTests");
    Directory.CreateDirectory(_tempPath);
    _appName = "TestApp";
    _filePrefix = _appName + FileLogger.PREFIX;
  }

  [Test]
  public async Task FileLogger_CreatesRightFileName()
  {
    var _fileLogger = new FileLogger(_tempPath, _appName);
    await _fileLogger.DisposeAsync();
    var partialTimestamp = DateTime.Now.ToString("yyyyMMddHH");
    var logFiles = Directory.GetFiles(_tempPath, $"{_filePrefix}_{partialTimestamp}*.txt");
    Assert.AreEqual(1, logFiles.Length, "Log file was not created.");
  }

  [Test]
  public async Task FileLogger_CreatesLogFile()
  {
    var _fileLogger = new FileLogger(_tempPath, _appName);
    await _fileLogger.DisposeAsync();
    Assert.IsTrue(Directory.GetFiles(_tempPath).Length == 1, "Log file was not created.");
  }

  [Test]
  public void FileLogger_NoLogFile_WhenUserLogOnDiskIsFalse()
  {
    var user = CreateStubUser(logOnDisk: false);
    var concierge = new Concierge(user.Object);
    concierge.Console.WriteLine("test");
    Assert.IsTrue(Directory.GetFiles(_tempPath).Length == 0, "Log file was created.");
  }

  [Test]
  public async Task FileLogger_LogFile_WhenUserLogOnDiskIsTrue()
  {
    var user = CreateStubUser(logOnDisk: true);
    var concierge = new Concierge(user.Object);
    concierge.Console.WriteLine("test");
    await concierge.DisposeAsync();
    Assert.IsTrue(Directory.GetFiles(_tempPath).Length == 1, "Log file was not created.");
  }

  private Mock<IUser> CreateStubUser(bool logOnDisk)
  {
    var user = new Mock<IUser>();
    user.Setup(x => x.IsConsoleWrittenToFile).Returns(logOnDisk);
    user.Setup(x => x.ConsoleFolderPath).Returns(_tempPath);
    user.Setup(x => x.AppName).Returns("whatever");
    return user;
  }

  [Test]
  public void FileLogger_ThrowsException_WhenLogPathIsNullOrEmpty()
  {
    Assert.Throws<ArgumentException>(() => new FileLogger(null, _appName));
    Assert.Throws<ArgumentException>(() => new FileLogger("", _appName));
  }

  [Test]
  public async Task FileLogger_CreatesDirectory_WhenItDoesNotExist()
  {
    var logPath = Path.Combine(_tempPath, "test");
    if (Directory.Exists(logPath))
    {
      Directory.Delete(logPath, true);
    }

    Assert.IsFalse(Directory.Exists(logPath), "Directory already exists.");
    var fileLogger = new FileLogger(logPath, _appName);
    await fileLogger.DisposeAsync();
    Assert.IsTrue(Directory.Exists(logPath), "Directory was not created.");
  }

  [Test]
  public async Task WriteAsync_WritesText_ToLogFile()
  {
    var logger = new FileLogger(_tempPath, _appName);
    const string text = "Test log entry";
    await logger.WriteAsync(text);
    await logger.DisposeAsync();
    var logContent = await File.ReadAllTextAsync(logger.filePath);
    Assert.IsTrue(logContent.Contains(text), "Text was not written to the log file.");
  }


  [Test]
  public async Task WriteAsync_WritesText_two_time_ToLogFile()
  {
    var logger = new FileLogger(_tempPath, _appName);
    const string text1 = "Test log entry1";
    const string text2 = "Test log entry2";
    await logger.WriteAsync(text1);
    await logger.WriteAsync(text2);
    await logger.DisposeAsync();

    var logContent = await File.ReadAllTextAsync(logger.filePath);

    Assert.IsTrue(logContent.Contains(text1), "Text was not written to the log file.");
    Assert.IsTrue(logContent.Contains(text2), "Text was not written to the log file.");
  }

  [Test]
  public async Task RotateLogs_DeletesOldFiles_WhenMoreThanLimit()
  {
    for (var i = 0; i < FileLogger.MAX_LOG_FILES + 5; i++)
    {
      var timestamp = DateTime.Now.AddMinutes(-50 + i).ToString("yyyyMMddHHmmss");
      var fileName = $"{_filePrefix}_{timestamp}.txt";
      var filePath = Path.Combine(_tempPath, fileName);
      File.WriteAllText(filePath, "Test log entry");
    }

    var logger = new FileLogger(_tempPath, _appName);
    await logger.DisposeAsync();
    var logFiles = Directory.GetFiles(_tempPath, $"{_filePrefix}_*.txt");
    Assert.AreEqual(FileLogger.MAX_LOG_FILES, logFiles.Length, "Log rotation did not delete old files as expected.");
  }

  [TearDown]
  public void TearDown()
  {
    var testLogFiles = Directory.GetFiles(_tempPath);
    foreach (var file in testLogFiles)
    {
      File.Delete(file);
    }
  }
}