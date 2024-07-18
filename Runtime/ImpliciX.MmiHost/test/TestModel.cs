using ImpliciX.Language.Model;

namespace ImpliciX.MmiHost.Tests
{
    public class TestModel:RootModelNode
    {
        public static SoftwareDeviceNode FooSoftware { get; } =
            new SoftwareDeviceNode(nameof(FooSoftware), new TestModel());

        public static SoftwareDeviceNode BarSoftware { get; } =
            new SoftwareDeviceNode(nameof(BarSoftware), new TestModel());
        
        
        public TestModel() : base(nameof(TestModel))
        {
        }
    }
}