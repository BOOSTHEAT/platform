using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImpliciX.Data.Api;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.LiveMenu
{
    public class LiveUpdateSoftware : ActionMenuViewModel<ILightConcierge>
    {
        public LiveUpdateSoftware(ILightConcierge concierge) : base(concierge)
        {
            Text = "Update software...";
        }

        public override async void Open()
        {
            await BusyWhile(async () =>
            {
                try
                {
                    var file = await Concierge.User.OpenFile(new IUser.FileSelection
                    {
                        Title = "Select software package for update",
                        Filters = new List<IUser.FileSelectionFilter>
                        {
                            new() { Name = "ZIP files (.zip)", Extensions = new List<string> { "zip" } },
                            new() { Name = "All files", Extensions = new List<string> { "*" } }
                        }
                    });
                    if (file.Choice != IUser.ChoiceType.Ok)
                        return;
                    await RemoteDeviceUpdater.UploadAndStartUpdate(file.Paths.First(), Concierge);
                }
                catch (Exception e)
                {
                    await Errors.Display(e);
                }
            });
        }
    }

    public static class RemoteDeviceUpdater
    {
        public static async Task UploadAndStartUpdate(string file, ILightConcierge concierge)
        {
            const string pathOnDevice = "/opt/package.zip";
            concierge.Console.WriteLine("Upload started");
            await concierge.RemoteDevice.Upload(file, pathOnDevice);
            concierge.Console.WriteLine("Upload complete");
            var updateMessage = WebsocketApiV2.CommandMessage.WithParameter("device:software:UPDATE", pathOnDevice).ToJson();
            await concierge.RemoteDevice.Send(updateMessage);
            concierge.Console.WriteLine("Update started.");
            concierge.Console.WriteLine("PROGRESS AND COMPLETION NEED TO BE CHECKED MANUALLY.");
        }
    }
}