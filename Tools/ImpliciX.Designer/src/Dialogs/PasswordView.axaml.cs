#nullable disable
using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.ViewModels;

namespace ImpliciX.Designer.Dialogs;

public partial class PasswordView : UserControl, IFullApi<string>, ISetCloseAction
{
  public PasswordView()
  {
    InitializeComponent();
    Focus();
    ContentTextBox.Focus();
  }

  public void SetButtonResult(string bdName) => this._buttonResult = bdName;

  public string GetButtonResult() => this._buttonResult;

  public Task Copy()
  {
    IClipboard clipboard = TopLevel.GetTopLevel(this)!.Clipboard;
    string text = this.ContentTextBox.SelectedText;
    if (string.IsNullOrEmpty(text))
      text = this.DataContext is AbstractMsBoxViewModel dataContext ? dataContext.ContentMessage : (string) null;
    return clipboard?.SetTextAsync(text);
  }

  public void Close()
  {
    Action closeAction = this._closeAction;
    if (closeAction == null)
      return;
    closeAction();
  }
  
  public void CloseWindow(object sender, EventArgs eventArgs) => this.Close();
  public void SetCloseAction(Action closeAction) => this._closeAction = closeAction;
  
  private string _buttonResult;
  private Action _closeAction;

}
