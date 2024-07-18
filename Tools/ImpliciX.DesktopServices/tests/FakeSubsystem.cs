using ImpliciX.Language.Model;

namespace ImpliciX.DesktopServices.Tests;

public class fakeSubsystem : SubSystemNode
{
    public fakeSubsystem(ModelNode parent) : base(nameof(fakeSubsystem), parent)
    {
    }
}