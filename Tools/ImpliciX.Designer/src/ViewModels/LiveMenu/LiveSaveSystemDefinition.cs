using System;
using System.Collections.Generic;
using System.IO;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.LiveMenu
{
    public class LiveSaveSystemDefinition : ActionMenuViewModel<ILightConcierge>
    {
        public LiveSaveSystemDefinition(ILightConcierge concierge) : base(concierge)
        {
            Text = "Save target system definition...";
        }

        public override async void Open()
        {
            await BusyWhile(async () =>
            {
                try
                {
                    var file = await Concierge.User.SaveFile(new IUser.FileSelection
                    {
                        Title = Text,
                        Filters = new List<IUser.FileSelectionFilter>
                        {
                            new() { Name = "JSON files (.json)", Extensions = new List<string> { "json" } },
                            new() { Name = "All files", Extensions = new List<string> { "*" } }
                        },
                        InitialFileName = NormalizeFileName(Concierge.RemoteDevice.CurrentTargetSystem.Name) + "_system_info.json"
                    });
                    if (file.Choice != IUser.ChoiceType.Ok)
                        return;

                    Save(Concierge.RemoteDevice.CurrentTargetSystem.SystemInfo.ToString(), file.Path);
                }
                catch (Exception e)
                {
                    await Errors.Display(e);
                }
            });
        }

        private static string NormalizeFileName(string s) => s.ToLower().Replace(" ", "_");

        private void Save(string json, string path)
        {
            var file = new FileInfo(Path.Combine(path));
            File.WriteAllText(file.FullName, json);
        }
    }
}