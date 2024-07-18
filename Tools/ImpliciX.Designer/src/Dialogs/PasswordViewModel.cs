using MsBox.Avalonia.Dto;
using MsBox.Avalonia.ViewModels;

namespace ImpliciX.Designer.Dialogs;

public class PasswordViewModel : MsBoxCustomViewModel
{
  public PasswordViewModel(MessageBoxCustomParams @params) : base(@params)
  {
    _passwordMask = '*';
    _passChar = _passwordMask;
    _passwordRevealed = false;
  }

  public char? PassChar
  {
    get => _passChar;
    set
    {
      if (_passChar == value)
        return;
      _passChar = value;
      OnPropertyChanged();
    }
  }

  private char? _passChar;

  public bool IsPasswordRevealed
  {
    get => _passwordRevealed;
    set
    {
      if (_passwordRevealed == value)
        return;
      _passwordRevealed = value;
      OnPropertyChanged();
    }
  }

  private bool _passwordRevealed;

  public void PasswordRevealClick()
  {
    PassChar = IsPasswordRevealed ? null : _passwordMask;
  }
  private readonly char _passwordMask;
}