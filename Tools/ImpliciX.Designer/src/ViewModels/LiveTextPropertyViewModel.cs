using ImpliciX.Language.Model;

namespace ImpliciX.Designer.ViewModels;

public class LiveTextPropertyViewModel  : LiveSingleDataViewModel
{
  public LiveTextPropertyViewModel(
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
