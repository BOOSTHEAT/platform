using System;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;

namespace ImpliciX.Designer.ViewModels.ProjectMenu
{
  public class ProjectMakeViewModel : ActionMenuViewModel<IConcierge>
  {
    public ProjectMakeViewModel(IConcierge concierge) : base(concierge)
    {
      Text = "Rebuild Device Definition";
      IsEnabled = false;
      var rebuildOnlyAvailableOnSomeProjects = Concierge.ProjectsManager.Projects
        .Subscribe(op => IsEnabled = op.Match(
          () => false, 
          p => p.CanMakeMultipleTimes
          ));
    }

    public override async void Open()
    {
      await BusyWhile(async () =>
      {
        try
        {
          Concierge.ProjectsManager.LatestProject.Tap(p => p.Make());
        }
        catch (Exception e)
        {
          await Errors.Display(e);
        }
      });
    }
  }
}