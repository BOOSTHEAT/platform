using System;
using System.Threading.Tasks;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels.ProjectMenu
{
  public class RunGuiViewModel : ActionMenuViewModel<IConcierge>
  {
    public RunGuiViewModel(IConcierge concierge) : base(concierge)
    {
      Text = "Run GUI";
    }

    public override async void Open()
    {
      await BusyWhile(async () =>
      {
        await RunGuiFor(Concierge.ProjectsManager.LatestProject.GetValue());
      });
    }

    private async Task RunGuiFor(IManageProject project)
    {
      try
      {
        await project.RunGui();
      }
      catch (Exception e)
      {
        await Errors.Display(e);
      }
    }
  }
}