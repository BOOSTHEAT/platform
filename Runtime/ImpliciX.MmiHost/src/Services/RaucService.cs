using System;
using System.IO;
using ImpliciX.Language.Core;
using ImpliciX.MmiHost.DBusProxies;
using Tmds.DBus;
using static ImpliciX.MmiHost.Constants;

namespace ImpliciX.MmiHost.Services
{
    public static class RaucService
    {
        private enum FileMode
        {
            ReadWrite = 0,
            ReadOnly = 1
        }

        public static void MarkAsGood(string chmodUBootPartition)
        {
            SideEffect.TryRun(() =>
            {
                ChangeWriteProtectionOnUBootPartition(FileMode.ReadWrite, chmodUBootPartition);
                var systemConnection = Connection.System;
                var raucInstaller = systemConnection.CreateProxy<IRaucSlotProxy>(IRaucSlotProxy.ServiceName, IRaucSlotProxy.Path);
                var result = raucInstaller.MarkAsync(IRaucSlotProxy.MarkStateGood, IRaucSlotProxy.SlotIdentifierBooted).GetAwaiter().GetResult();
                Log.Debug("Mark As Good : {@MarkAsGoodMessage}", result.message);
                ChangeWriteProtectionOnUBootPartition(FileMode.ReadOnly, chmodUBootPartition);
            }, exception => Log.Error(exception, $"Cannot mark {IRaucSlotProxy.SlotIdentifierBooted}"));
        }

        private static void ChangeWriteProtectionOnUBootPartition(FileMode fileMode, string chmodUBootPartition)
        {
            SideEffect.TryRun(() => { File.WriteAllText(chmodUBootPartition, fileMode.ToString("d")); },
                exception => Log.Warning($"Unable to write file {chmodUBootPartition} with {fileMode}: {exception.Message}"));
        }

        public static Result<string> GetActivePartition()
        {
            return SideEffect.TryRun(() =>
            {
                var rauc = Connection.System.CreateProxy<IRaucSlotProxy>(IRaucSlotProxy.ServiceName, IRaucSlotProxy.Path);
                var primary = rauc.GetPrimaryAsync().GetAwaiter().GetResult();
                return primary;
                
            }, e => new Error("RAUC_ERROR", e.CascadeMessage()));
        }

        public static string GetOppositePartition(string partition)
        {
            var activePartition = partition;
            if (activePartition == BOOT_FS_0) return BOOT_FS_1;
            if (activePartition == BOOT_FS_1) return BOOT_FS_0;
            Log.Error($"No matching partition with name {activePartition}");
            return String.Empty;
        }

        public static void ChangeSlot()
        {
            SideEffect.TryRun(() =>
            {
                Log.Debug($"Set other slot active");
                var rauc = Connection.System.CreateProxy<IRaucSlotProxy>(IRaucSlotProxy.ServiceName, IRaucSlotProxy.Path);
                var (slotName, message) = rauc.MarkAsync(IRaucSlotProxy.MarkStateActive, IRaucSlotProxy.SlotIdentifierOther).GetAwaiter().GetResult();
                Log.Debug($"SlotName {slotName}, Message {message}");
            }, exception => Log.Error(exception, $"Error during changing slot of the system"));
        }
    }
}