using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using ImpliciX.DesktopServices;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels
{
  public class MenuItemViewModel : ViewModelBase
  {
    public bool IsEnabled
    {
      get => _isEnabled;
      set => this.RaiseAndSetIfChanged(ref _isEnabled, value);
    }
    private bool _isEnabled = true;
    
    public string Text
    {
      get => _text;
      set => this.RaiseAndSetIfChanged(ref _text, value);
    }
    private string _text = "Undefined";
    
    public ObservableCollection<MenuItemViewModel> Items
    {
      get => _items;
      set => this.RaiseAndSetIfChanged(ref _items, value);
    }
    private ObservableCollection<MenuItemViewModel> _items = new (Array.Empty<MenuItemViewModel>());

    public IEnumerable<MenuItemViewModel> StaticItems
    {
      set => Items = new(value);
    }
    public ICommand Command { get; set; }
    public object CommandParameter { get; set; }
  }

  public class MenuSeparatorViewModel : MenuItemViewModel
  {
    public MenuSeparatorViewModel()
    {
      Text = "-";
    }
  }

  public class CommandViewModel : MenuItemViewModel
  {
    public CommandViewModel(string text, Action command)
    {
      Text = text;
      Command = ReactiveCommand.Create(command);
    }
  }
  
  public abstract class ActionMenuViewModel<TConcierge> : MenuItemViewModel
  where TConcierge : ILightConcierge
  {
    protected ActionMenuViewModel(TConcierge concierge)
    {
      Concierge = concierge;
      Errors = new Errors(concierge);
      Command = ReactiveCommand.Create(Open);
      DefaultEnable = true;
      _isBusy = false;
    }

    protected bool DefaultEnable
    {
      get => _defaultEnable;
      set
      {
        _defaultEnable = value;
        IsEnabled = !_isBusy && _defaultEnable;
      }
    }
    private bool _isBusy;
    private bool _defaultEnable;
    protected readonly Errors Errors;
    public readonly TConcierge Concierge;

    public abstract void Open();
  
    protected async Task BusyWhile(Func<Task> a)
    {
      _isBusy = true;
      IsEnabled = false;
      await a();
      _isBusy = false;
      IsEnabled = DefaultEnable;
    }
    
  }
}