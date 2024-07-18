namespace ImpliciX.MmiHost
{
    public class Constants
    {
        public const string ENV_VARIABLE_CURRENT_SLOT_BSP_VERSION = "BSP_VERSION_ID";
        public const string ENV_VARIABLE_OTHER_SLOT_BSP_VERSION = "BSP_OTHER_VERSION_ID";
        public const string FORCE_RO_FILE = "/sys/block/mmcblk0boot0/force_ro";
        public const string BOOT_FS_0 = "bootfs.0";
        public const string BOOT_FS_1 = "bootfs.1";
        public const string BACKLIGHT_FILE_PATH = "/sys/class/backlight/backlight/brightness";
        public const string SOFTWARE_INSTALLATION_PATH = "/opt/software/";
        public const string BOOT_FS_PATH = "/opt/slot/";
        public const string BOILER_APP_UNIT_NAME = "boiler_app.service";
    }
}