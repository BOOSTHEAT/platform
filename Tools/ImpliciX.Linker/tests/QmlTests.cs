using System.Linq;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Linker.Tests;

public class QmlTests
{
    [Test]
    public void WhenICreateCommand_ThenCommandContainsNugetSourceOption()
    {
        Check.That(Qml
                .CreateCommand()
                .Options
                .Any(o => o.Aliases.Any(a => string.Equals(a, "-s"))))
            .IsTrue();
    }
}