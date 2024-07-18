using ImpliciX.Language.Model;

namespace ImpliciX.Designer.ViewModels;

public  class LiveEnumPropertyViewModel : LiveSingleDataViewModel
{
  public LiveEnumPropertyViewModel(
    Urn urn,
    LivePropertiesViewModel parent,
    bool inModel
  ) : base(
    urn,
    parent,
    inModel
  )
  {
  }
}
