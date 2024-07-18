using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;
using JetBrains.Annotations;

namespace ImpliciX.Designer.ViewModels
{
  public class Errors
  {
    public Errors([NotNull] ILightConcierge concierge)
    {
      Concierge = concierge ?? throw new ArgumentNullException(nameof(concierge));
    }

    [NotNull] public readonly ILightConcierge Concierge;

    public async Task Display([NotNull] Exception e, bool displayStackTrace = false)
    {
      if (e == null) throw new ArgumentNullException(nameof(e));

      if (e.InnerException == null)
      {
        await DisplayCascade(e, displayStackTrace);
        return;
      }

      var wantDetails = await Display("Failed", e.Message, new[]
        {
          new IUser.Choice { Type = IUser.ChoiceType.Ok },
          new IUser.Choice { Type = IUser.ChoiceType.Custom1, Text = "Details" },
        }
      );
      if (wantDetails == IUser.ChoiceType.Custom1)
        await DisplayCascade(e.InnerException, displayStackTrace);
    }

    private async Task DisplayCascade(Exception e, bool displayStackTrace)
    {
      var message = e.CascadeMessage();
      if (displayStackTrace)
        message += $"====STACK TRACE ===={Environment.NewLine}{e.StackTrace}";

      Concierge.Console?.WriteLine($"{e.Message}\n{e.StackTrace}");
      await Display("Failed", message);
    }

    public async Task<IUser.ChoiceType>
      Display(string title, string message, IEnumerable<IUser.Choice> choices = null) =>
      await Concierge.User.Show(new IUser.Box
      {
        Title = title,
        Message = message,
        Icon = IUser.Icon.Error,
        Buttons = choices ?? IUser.StandardButtons(IUser.ChoiceType.Ok),
      });
  }
}