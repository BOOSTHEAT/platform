using ImpliciX.Language.Model;

namespace ImpliciX.Designer.Tests
{
    public class fakeSubsystem : SubSystemNode
    {
        public fakeSubsystem(ModelNode parent) : base(nameof(fakeSubsystem), parent)
        {
        }
    }
}