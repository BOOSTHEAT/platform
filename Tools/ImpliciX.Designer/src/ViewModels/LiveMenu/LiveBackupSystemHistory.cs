using System.IO;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.LiveMenu
{
  public class LiveBackupSystemHistory : ConditionalMenuViewModel<ITargetSystem>
  {
    private const string JournalName = "log.txt.gz";
    public LiveBackupSystemHistory(ILightConcierge concierge) : base(
      concierge,
      "Backup system history...",
      concierge.RemoteDevice.TargetSystem,
      ts => ts.SystemHistoryBackup.IsAvailable || ts.SystemJournalBackup.IsAvailable,
      async (m, ts) =>
      {
        var folder = await m.Concierge.User.OpenFolder(new IUser.FileSelection
        {
          Title = "Select backup destination folder",
        });
        if (folder.Choice != IUser.ChoiceType.Ok)
          return;
        m.Concierge.Console.WriteLine("Starting backup of system history");
        if (ts.SystemJournalBackup.IsAvailable)
        {
          await ts.SystemJournalBackup.Execute().AndSaveTo(Path.Combine(folder.Path,JournalName));
          m.Concierge.Console.WriteLine($"Journal {JournalName} into {folder.Path}");
        }
        if (ts.SystemHistoryBackup.IsAvailable)
        {
          //TODO : Maybe add checksum integrity checking (as DownloadMetrics)
          await foreach (var (count, length, name, _) in ts.SystemHistoryBackup.Execute().AndSaveManyTo(folder.Path))
            m.Concierge.Console.WriteLine($"[{count}/{length}] {name} into {folder.Path}");
        }
        m.Concierge.Console.WriteLine("Backup of system history complete");
      }
      )
    {
    }
  }
}