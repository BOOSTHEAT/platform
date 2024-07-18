using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels
{
  public class IdentityViewModel : ViewModelBase
  {
    public IdentityViewModel(ILightConcierge concierge)
    {
      Items = new ActionMenuViewModel<ILightConcierge>[]
      {
        new ExecuteAction(concierge, "Create...", async () =>
        {
          await concierge.Identity.Create();
        }),
        new ExecuteAction(concierge, "Export...", async () =>
        {
          var identity = await concierge.Identity.Read();
          if (identity == null)
            return;
          var folder = await concierge.User.OpenFolder(new IUser.FileSelection
          {
            Title = "Select destination folder",
            Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
          });
          if (folder.Choice != IUser.ChoiceType.Ok)
            return;
          await File.WriteAllTextAsync(Path.Combine(folder.Path, $"{concierge.Identity.Name}.identity"),
            identity.Value.Key);
          await File.WriteAllTextAsync(Path.Combine(folder.Path, $"{concierge.Identity.Name}.identity.pub"),
            $"{identity.Value.File.ToPublic()} {concierge.Identity.Name}");
          concierge.Console.WriteLine($"Exported identity to {folder}");
        }),
        new ExecuteAction(concierge, "Import...", async () =>
        {
          var file = await concierge.User.OpenFile(new IUser.FileSelection
          {
            AllowMultiple = false,
            Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Filters = new List<IUser.FileSelectionFilter>
            {
              new IUser.FileSelectionFilter
                {Name = "Identity files (.identity)", Extensions = new List<string> {"identity"}},
              new IUser.FileSelectionFilter {Name = "All files", Extensions = new List<string> {"*"}}
            }
          });
          if (file.Choice != IUser.ChoiceType.Ok)
            return;
          await concierge.Identity.Import(await File.ReadAllTextAsync(file.Paths.First()));
        })
      };
    }

    public IEnumerable<MenuItemViewModel> Items { get; }

    class ExecuteAction : ActionMenuViewModel<ILightConcierge>
    {
      private readonly Func<Task> _action;

      public ExecuteAction(ILightConcierge concierge, string text, Func<Task> action) : base(concierge)
      {
        _action = action;
        Text = text;
      }

      public override async void Open()
      {
        await BusyWhile(async () =>
        {
          try
          {
            await _action();
          }
          catch (Exception e)
          {
            await Errors.Display(e);
          }
        });
      }
    }
  }
}
