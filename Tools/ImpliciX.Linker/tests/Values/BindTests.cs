using System.IO;
using ImpliciX.Linker.Values;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests.Values;

public class BindTests
{
    [TestCase("./test_artifacts/testfile.txt", "to/dir")]
    [TestCase("./test_artifacts", "to/dir")]
    [TestCase("./test_artifacts/", "to/dir/")]
    public void Parse(string from, string dest)
    {
        var definition = from + ":" + dest;
        Assert.IsFalse(Bind.IsInvalid(definition));
        var bd = new Bind(definition);
        Assert.That(bd.SourcePath.FullName, Is.EqualTo(new FileInfo(from).FullName));
        Assert.That(bd.DestinationPath, Is.EqualTo(dest));
    }

    [TestCase("./test_artifacts/notfound.txt")]
    [TestCase("./test_artifacts/notfound.xx")]
    [TestCase("./not_existing_dir")]
    [TestCase("./not_existing_dir/")]
    public void Invalid(string from)
    {
        var definition = from + ":to/dir";
        Assert.IsTrue(Bind.IsInvalid(definition));
    }
}