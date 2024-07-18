using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImpliciX.Designer.ViewModels.LiveMenu;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;

namespace ImpliciX.Designer.ViewModels.ProjectMenu
{
    public class PackageCreationViewModel : ActionMenuViewModel<IConcierge>
    {
        public PackageCreationViewModel(IConcierge concierge) : base(concierge)
        {
            Text = "Create ImpliciX Package";
        }

        public override async void Open()
        {
            await BusyWhile(async () => { await CreatePackageFor(Concierge.ProjectsManager.LatestProject.GetValue()); });
        }

        private async Task CreatePackageFor(IManageProject project)
        {
            try
            {
                var remoteDeviceIsConnected = !string.IsNullOrEmpty(Concierge.RemoteDevice.IPAddressOrHostname);

                var currentSystem = remoteDeviceIsConnected
                    ? Concierge.RemoteDevice.CurrentTargetSystem.SystemInfo
                    : await LoadSystemInfo();

                if (currentSystem.IsNone)
                    return;
                
                var publishDirectory = await project.CreatePackage(new SystemInfo(currentSystem.GetValue().Os, currentSystem.GetValue().Architecture, currentSystem.GetValue().Hardware));

                if (remoteDeviceIsConnected)
                    if (await ConfirmAndUploadPackage(publishDirectory))
                        return;

                await ShowPackageCreatedNotification(publishDirectory);
            }
            catch (Exception e)
            {
                await Errors.Display(e);
            }
        }

        private async Task<Option<SystemInfo>> LoadSystemInfo()
        {
            var file = await Concierge.User.OpenFile(new IUser.FileSelection
            {
                Title = "Select software package for update",
                Filters = new List<IUser.FileSelectionFilter>
                {
                    new() { Name = "Json files (.json)", Extensions = new List<string> { "json" } },
                    new() { Name = "All files", Extensions = new List<string> { "*" } }
                }
            });
            if (file.Choice != IUser.ChoiceType.Ok)
                return Option<SystemInfo>.None();

            var json = await File.ReadAllTextAsync(file.Paths.First());
            return SystemInfo.FromString(json);
        }


        private async Task ShowPackageCreatedNotification(FileInfo publishDirectory)
        {
            var def = new IUser.Box
            {
                Title = "ImpliciX Package created",
                Message = $"The package was created at {publishDirectory.FullName}",
                Icon = IUser.Icon.Success,
                Buttons = IUser.StandardButtons(IUser.ChoiceType.Ok),
            };
            await Concierge.User.Show(def);
        }

        private async Task<bool> ConfirmAndUploadPackage(FileSystemInfo publishDirectory)
        {
            var box = new IUser.Box
            {
                Title = "Uploading Package",
                Message = @"Do you want to upload the created package to the remote device?",
                Icon = IUser.Icon.Success,
                Buttons = IUser.StandardButtons(IUser.ChoiceType.Yes, IUser.ChoiceType.No)
            };
            var choice = await Concierge.User.Show(box);
            if (choice != IUser.ChoiceType.Yes) return false;
            await RemoteDeviceUpdater.UploadAndStartUpdate(publishDirectory.FullName, Concierge);
            return true;
        }
    }
}