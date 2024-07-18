using System.Collections.Generic;
using System.Linq;
using ImpliciX.Designer.Dialogs;
using ImpliciX.DesktopServices;
using ReactiveUI;

namespace dialogs_demo;

public class Data : ReactiveObject
{
  public Data()
  {
    Message = new MessageBoxes();
    _result = "";
    _selectedIconIndex = 0;
    _selectedButtonsIndex = 0;
    _theTitle = "The Title";
    _theMessage = "The Message";
  }

  public MessageBoxes Message { get; }

  public string Result
  {
    get => _result;
    set => this.RaiseAndSetIfChanged(ref _result, value);
  }

  private string _result;

  public string TheTitle
  {
    get => _theTitle;
    set => this.RaiseAndSetIfChanged(ref _theTitle, value);
  }

  private string _theTitle;

  public string TheMessage
  {
    get => _theMessage;
    set => this.RaiseAndSetIfChanged(ref _theMessage, value);
  }

  private string _theMessage;

  public int SelectedIconIndex
  {
    get => _selectedIconIndex;
    set => this.RaiseAndSetIfChanged(ref _selectedIconIndex, value);
  }

  private int _selectedIconIndex;

  public IUser.Icon[] IconChoice => new[]
  {
    IUser.Icon.None,
    IUser.Icon.Error,
    IUser.Icon.Info,
    IUser.Icon.Setting,
    IUser.Icon.Stop,
    IUser.Icon.Success,
    IUser.Icon.Warning
  };

  public int SelectedButtonsIndex
  {
    get => _selectedButtonsIndex;
    set => this.RaiseAndSetIfChanged(ref _selectedButtonsIndex, value);
  }

  private int _selectedButtonsIndex;

  public string[] ButtonsChoiceDisplay => ButtonsChoice.Select(bc =>
    string.Join(',', bc.Select(c =>
      $"[{(c.IsDefault?"*":"")}{(c.IsCancel?"#":"")}{c.Type}{(string.IsNullOrEmpty(c.Text)?"":" ")+c.Text}]"))
  ).ToArray();
  
  public IEnumerable<IUser.Choice>[] ButtonsChoice => new[]
  {
    IUser.StandardButtons(IUser.ChoiceType.Ok),
    IUser.StandardButtons(IUser.ChoiceType.Ok, IUser.ChoiceType.Cancel),
    IUser.StandardButtons(IUser.ChoiceType.Yes, IUser.ChoiceType.No),
    IUser.StandardButtons(IUser.ChoiceType.Yes, IUser.ChoiceType.No, IUser.ChoiceType.Cancel),
    IUser.StandardButtons(IUser.ChoiceType.Ok, IUser.ChoiceType.Abort),
    IUser.StandardButtons(IUser.ChoiceType.Yes, IUser.ChoiceType.No, IUser.ChoiceType.Abort),
    IUser.StandardButtons(IUser.ChoiceType.Ok, IUser.ChoiceType.Yes),
    IUser.ChoiceType.Custom1.With(text: "Foo") + IUser.ChoiceType.Custom2.With(text: "Bar"),
    IUser.ChoiceType.Ok.With(isDefault: true)
    + IUser.ChoiceType.Cancel.With(isCancel: true)
    + IUser.ChoiceType.Custom1.With(text: "Foo")
    + IUser.ChoiceType.Custom2.With(text: "Bar")
    + IUser.ChoiceType.Custom3.With(text: "Qix"),
    IUser.ChoiceType.Custom1.With(text:"Vazy", isDefault:true) + IUser.ChoiceType.Cancel.With(isCancel:true)
  };

  public async void ShowMessageBox()
  {
    var box = new IUser.Box
    {
      Title = TheTitle,
      Message = TheMessage,
      Icon = IconChoice[SelectedIconIndex],
      Buttons = ButtonsChoice[SelectedButtonsIndex]
    };
    var result = await Message.Show(box);
    Result = result.ToString();
  }

  public async void EnterPassword()
  {
    var box = new IUser.Box
    {
      Title = TheTitle,
      Message = TheMessage,
      Icon = IconChoice[SelectedIconIndex],
      Buttons = ButtonsChoice[SelectedButtonsIndex]
    };
    var result = await Message.EnterPassword(box);
    Result = result.ToString();
  }
}