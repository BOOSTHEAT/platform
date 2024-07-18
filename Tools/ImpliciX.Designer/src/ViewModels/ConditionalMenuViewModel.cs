using System;
using System.Threading.Tasks;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.ViewModels;

public class ConditionalMenuViewModel<T> : ActionMenuViewModel<ILightConcierge>
{
  public ConditionalMenuViewModel(ILightConcierge concierge, string text,
    IObservable<T> reference,
    Func<T,bool> isAvailable,
    Func<ConditionalMenuViewModel<T>,T,Task> execute)
  : base(concierge)
  {
    Text = text;
    DefaultEnable = false;
    _execute = execute;
    var availability = reference
      .Subscribe(item =>
      {
        _item = item;
        DefaultEnable = item!=null && isAvailable(item);
      });
  }
  private readonly Func<ConditionalMenuViewModel<T>,T,Task> _execute;
  private T _item;

  public override void Open()
  {
    OpenAsync();
  }

  public Task OpenAsync()
  {
    return BusyWhile(async () =>
    {
      try
      {
        await _execute(this, _item);
      }
      catch (Exception e)
      {
        await Errors.Display(e);
      }
    });
  }
  
  public async Task<bool> Ask(string title, string question)
  {
    var def = new IUser.Box
    {
      Title = title,
      Message = question,
      Icon = IUser.Icon.Stop,
      Buttons = IUser.StandardButtons(IUser.ChoiceType.Ok, IUser.ChoiceType.Cancel),
    };
    var result = await Concierge.User.Show(def);
    return result is IUser.ChoiceType.Ok;
  }
}