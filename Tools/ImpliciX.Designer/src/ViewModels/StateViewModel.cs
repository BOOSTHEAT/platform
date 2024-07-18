using ReactiveUI;

namespace ImpliciX.Designer.ViewModels
{
  public class StateViewModel : BaseStateViewModel
  {
    public StateViewModel(string name, int index, DefinitionViewModel definition) : base(name,index,definition)
    {
    }

    private bool _isActive;
    public bool IsActive
    {
      get => _isActive;
      set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

  }
}