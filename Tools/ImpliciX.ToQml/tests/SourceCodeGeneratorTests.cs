using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

[TestFixture]
public class SourceCodeGeneratorTests
{
  [Test]
  public void SourceCodeIsRowsOfText()
  {
    var scg = new SourceCodeGenerator();
    scg.Append("foo").Append("bar");
    Assert.That(scg.Result, Is.EqualTo("foo\nbar\n"));
  }
  
  [Test]
  public void AddMultipleRowsAtOnce()
  {
    var scg = new SourceCodeGenerator();
    scg.Append("foo","bar");
    Assert.That(scg.Result, Is.EqualTo("foo\nbar\n"));
  }
  
  [Test]
  public void WriteAnythingConvertibleToString()
  {
    var scg = new SourceCodeGenerator();
    scg.Append(1,2,3);
    Assert.That(scg.Result, Is.EqualTo("1\n2\n3\n"));
  }
  
  [Test]
  public void AppendWithCondition()
  {
    var z = 0;
    var scg = new SourceCodeGenerator();
    scg.Append(false, () => 2/z );
    scg.Append(true, () => 6);
    Assert.That(scg.Result, Is.EqualTo("6\n"));
  }

  [Test]
  public void SublevelIndentation()
  {
    var scg = new SourceCodeGenerator();
    scg.Open("yolo").Append("foo").Append("bar").Close();
    Assert.That(scg.Result, Is.EqualTo("yolo {\n  foo\n  bar\n}\n"));
  }

  [Test]
  public void MultiplelevelsOfIndentation()
  {
    var scg = new SourceCodeGenerator();
    // @formatter:off
    scg.Open("yolo")
      .Append("wat")
      .Open("Fizz")
        .Append("foo")
        .Append("bar")
      .Close()
      .Open("Buzz")
        .Append("qix")
      .Close()
    .Close();
    // @formatter:on
    Assert.That(scg.Result, Is.EqualTo("yolo {\n  wat\n  Fizz {\n    foo\n    bar\n  }\n  Buzz {\n    qix\n  }\n}\n"));
  }
}