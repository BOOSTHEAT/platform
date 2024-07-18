using System;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels
{
  public class OpenUrlActionMenu : ActionMenuViewModel<ILightConcierge>
  {
    private readonly string _url;

    public OpenUrlActionMenu(ILightConcierge concierge, string title, string url) : base(concierge)
    {
      _url = url;
      Text = title;
    }

    
    public override async void Open()
    {
      await BusyWhile(async () =>
      {
        try
        {
          await Concierge.OperatingSystem.OpenUrl(_url);
        }
        catch (Exception e)
        {
          await Errors.Display(e);
        }
      });
    }
  }
}