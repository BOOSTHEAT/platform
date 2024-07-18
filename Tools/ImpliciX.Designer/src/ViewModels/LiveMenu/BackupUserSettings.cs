using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.ViewModels.LiveMenu
{
    public class BackupSettings : ActionMenuViewModel<ILightConcierge>
    {
        public string Filename { get; }
        public Func<IEnumerable<Urn>> Settings { get; }

        public BackupSettings(ILightConcierge concierge, string text, string filename, Func<IEnumerable<Urn>> settings) : base(concierge)
        {
            Filename = filename;
            Settings = settings;
            Text = text;
        }

        public override async void Open()
        {
            var settings = Settings();
            if (settings == null)
            {
                await Errors.Display("Error", "Cannot backup settings without a device definition");
                return;
            }
            var file = await Concierge.User.SaveFile(new IUser.FileSelection
            {
                Title = Text,
                Filters = new List<IUser.FileSelectionFilter>
                {
                    new IUser.FileSelectionFilter { Name = "CSV files (.csv)", Extensions = new List<string> { "csv" } },
                    new IUser.FileSelectionFilter { Name = "All files", Extensions = new List<string> { "*" } }
                },
                InitialFileName = Filename
            });
            if (file.Choice != IUser.ChoiceType.Ok)
                return;
            IEnumerable<(string urn, string value)> GetPropertyValue(string key)
            {
                var v = Concierge.Session.Properties.Lookup(key);
                if (v.HasValue)
                    yield return (key, v.Value.Value);
            }
            var userProperties = settings
                .SelectMany(u => GetPropertyValue(u.Value))
                .Select(p => $"00:00:00,properties,{p.urn},{p.value}");
            await File.WriteAllLinesAsync(file.Path, userProperties);
        }
    }
}