using ImpliciX.Language.Model;

namespace ImpliciX.Data.Tests
{
    public class dummy:RootModelNode
    {


        public dummy() : base(nameof(dummy))
        {
        }

        public static SoftwareDeviceNode mcu1 => new SoftwareDeviceNode(nameof(mcu1), new dummy());
        public static SoftwareDeviceNode mcu2 => new SoftwareDeviceNode(nameof(mcu2), new dummy());
        public static SoftwareDeviceNode mcu3 => new SoftwareDeviceNode(nameof(mcu3), new dummy());
        public static SoftwareDeviceNode app1 => new SoftwareDeviceNode(nameof(app1), new dummy());
        public static SoftwareDeviceNode bsp => new SoftwareDeviceNode(nameof(bsp), new dummy());

    }
}