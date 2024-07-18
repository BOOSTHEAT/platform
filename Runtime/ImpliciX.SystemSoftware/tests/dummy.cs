using ImpliciX.Language.Model;

namespace ImpliciX.SystemSoftware.Tests
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
        public static PropertyUrn<SoftwareVersion> release_version => PropertyUrn<SoftwareVersion>.Build(nameof(dummy), nameof(release_version));
        public static PropertyUrn<UpdateState> update_state => PropertyUrn<UpdateState>.Build(nameof(dummy), nameof(update_state));
 
        public static CommandNode<PackageLocation> _update => CommandNode<PackageLocation>.Create("UPDATE", new dummy());

        public static CommandNode<NoArg> _clean_version_settings => CommandNode<NoArg>.Create("clean_settings", new dummy());
        public static CommandNode<NoArg> _commit => CommandNode<NoArg>.Create("commit", new dummy());
        public static CommandNode<NoArg> _reboot => CommandNode<NoArg>.Create("reboot", new dummy());
    }
}