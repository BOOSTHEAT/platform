using ImpliciX.DesktopServices.Services;
using NFluent;

namespace ImpliciX.DesktopServices.Tests.Services;

public class ConsoleOutputSliceTests
{
  [Test]
  public async Task RecordConsoleLinesFromSliceCreation()
  {
    var console = new ConsoleService();
    console.WriteLine("qix");
    var sut = IConsoleOutputSlice.Create(console);
    console.WriteLine("foo");
    console.WriteLine("bar");
    using var stream = new MemoryStream();
    await sut.DumpInto(stream);
    Check.That(ReadAll(stream)).IsEqualTo($"foo{Environment.NewLine}bar{Environment.NewLine}");
  }

  [Test]
  public async Task RecordConsoleErrorsFromSliceCreation()
  {
    var console = new ConsoleService();
    console.WriteError(new Exception("qix"));
    var sut = IConsoleOutputSlice.Create(console);
    console.WriteError(new Exception("foo"));
    console.WriteError(new Exception("bar"));
    using var stream = new MemoryStream();
    await sut.DumpInto(stream);
    Check.That(ReadAll(stream)).IsEqualTo($"foo{Environment.NewLine}bar{Environment.NewLine}");
  }

  private static string ReadAll(MemoryStream stream)
  {
    stream.Position = 0;
    using var reader = new StreamReader(stream);
    var actual = reader.ReadToEnd();
    return actual;
  }
}