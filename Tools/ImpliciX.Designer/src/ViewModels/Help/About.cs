using System;
using System.Reflection;
using ImpliciX.DesktopServices;
using ImpliciX.Language;

namespace ImpliciX.Designer.ViewModels.Help
{
    public class About : ActionMenuViewModel<ILightConcierge>
    {
        private readonly string _title;

        public About(ILightConcierge concierge, string title) : base(concierge)
        {
            _title = title;
            Text = "About";
        }

        public override async void Open()
        {
            await BusyWhile(async () =>
            {
                try
                {
                    var def = new IUser.Box
                    {
                        Title = _title,
                        Message = $"Version: {Version(Assembly.GetEntryAssembly())}\n" +
                                         $"Language: {Version(typeof(ApplicationDefinition).Assembly)}\n\n" +
                                         $"IP Addresses: {String.Join(", ", Concierge.RemoteDevice.LocalIPAddresses)}",
                        Icon = IUser.Icon.Info,
                        Buttons = IUser.StandardButtons(IUser.ChoiceType.Ok),
                    };
                    await Concierge.User.Show(def);
                }
                catch (Exception e)
                {
                    await Errors.Display(e);
                }
            });
        }

        private string Version(Assembly a) => a.GetName().Version.ToString();
    }
}