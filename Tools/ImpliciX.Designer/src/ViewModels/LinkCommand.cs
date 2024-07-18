using System;

namespace ImpliciX.Designer.ViewModels;

public class LinkCommand : ViewModelBase
{
  private readonly Action _command;

  public LinkCommand(string text, Action command)
  {
    Text = text;
    _command = command;
  }

  public string Text { get; }
  public void Command() => _command();
}