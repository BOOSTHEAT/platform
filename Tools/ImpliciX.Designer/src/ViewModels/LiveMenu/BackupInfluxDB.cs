using System;
using System.IO;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.LiveMenu
{
    public class BackupInfluxDB : ActionMenuViewModel<ILightConcierge>
    {
        public BackupInfluxDB(ILightConcierge concierge) : base(concierge)
        {
            Text = "Backup InfluxDB...";
        }

        public override async void Open()
        {
            await BusyWhile(async () =>
            {
                try
                {
                    var folder = await Concierge.User.OpenFolder(new IUser.FileSelection
                    {
                        Title = "Select backup destination folder",
                    });
                    if (folder.Choice != IUser.ChoiceType.Ok)
                        return;
                    Concierge.Console.WriteLine("Starting backup of InfluxDB");
                    await Concierge.RemoteDevice.CurrentTargetSystem.InfluxDbBackup.Execute().AndSaveTo(
                        Path.Combine(folder.Path,
                            $"{Concierge.RemoteDevice.IPAddressOrHostname}.{DateTime.Now.ToString("yyyy'-'MM'-'dd'-'HH'-'mm'-'ss")}.influxdb.tar.gz")
                        );
                    Concierge.Console.WriteLine("Backup of InfluxDB complete");
                }
                catch (Exception e)
                {
                    await Errors.Display(e);
                }
            });
        }
    }
}