using System;
using System.Linq;
using System.Text.RegularExpressions;
using ImpliciX.Language.Core;
using NUnit.Framework;

namespace ImpliciX.Data.Tests;

[SetUpFixture]
public class Fixture
{
  [OneTimeSetUp]
  public void OneTimeSetUp()
  {
    SetupNUnitLogger();
  }

  public static void SetupNUnitLogger()
  {
    Log.Logger = new NUnitLogger();
  }
}

public abstract class StdLogger : ILog
{
  protected abstract void Out(string level, string message);
  protected abstract void Out(string level, string messageTemplate, params object[] args);
  protected abstract void Err(string level, string message);
  protected abstract void Err(string level, string messageTemplate, params object[] args);
  protected abstract void Err(string level, Exception exception, string messageTemplate, params object[] args);

  public void Verbose(string message) => Out("VERBOSE", message);
  public void Verbose(string messageTemplate, params object[] args) => Out("VERBOSE", messageTemplate, args);
  public void Debug(string message) => Out("DEBUG", message);
  public void Debug(string messageTemplate, params object[] args) => Out("DEBUG", messageTemplate, args);
  public void Information(string message) => Out("INFORMATION", message);
  public void Information(string messageTemplate, params object[] args) => Out("INFORMATION", messageTemplate, args);

  public void Warning(string message) => Err("WARNING", message);
  public void Warning(string messageTemplate, params object[] args) => Err("WARNING", messageTemplate, args);
  public void Error(string message) => Err("ERROR", message);
  public void Error(string messageTemplate, params object[] args) => Err("ERROR", messageTemplate, args);
  public void Error(Exception exception, string message) => Err("ERROR", exception, message);

  public void Error(Exception exception, string messageTemplate, params object[] args) =>
    Err("ERROR", exception, messageTemplate, args);

  public void Fatal(string message) => Err("FATAL", message);
  public void Fatal(string messageTemplate, params object[] args) => Err("FATAL", messageTemplate, args);
  public void Fatal(Exception exception, string message) => Err("FATAL", exception, message);

  public void Fatal(Exception exception, string messageTemplate, params object[] args) =>
    Err("FATAL", exception, messageTemplate, args);
}

public class NUnitLogger : StdLogger
{
  protected override void Out(string level, string message) => TestContext.Out.WriteLine(LevelMessage(level, message));

  protected override void Out(string level, string messageTemplate, params object[] args) =>
    TestContext.Out.WriteLine(LevelMessage(level, PrepareTemplate(messageTemplate)), args);

  protected override void Err(string level, string message) => TestContext.Error.WriteLine(LevelMessage(level, message));

  protected override void Err(string level, string messageTemplate, params object[] args) =>
    TestContext.Error.WriteLine(LevelMessage(level, PrepareTemplate(messageTemplate)), args);

  protected override void Err(string level, Exception exception, string messageTemplate, params object[] args)
  {
    TestContext.Error.WriteLine(LevelMessage(level, PrepareTemplate(messageTemplate)), args);
    TestContext.Error.WriteLine(exception.Message);
    TestContext.Error.WriteLine(exception.StackTrace);
  }

  private static string LevelMessage(string level, string message) => $"[{level}] {message}";

  private string PrepareTemplate(string messageTemplate)
  {
    var matches = tmpl.Matches(messageTemplate).Select((m,i) => (m.Value, i));
    var result = matches.Aggregate(messageTemplate, (s, m) => s.Replace(m.Value, $"{{{m.i}}}"));
    return result;
  }

  private static Regex tmpl = new Regex("{[^}]+}");
}