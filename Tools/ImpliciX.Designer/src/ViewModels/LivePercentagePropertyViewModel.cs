using ImpliciX.Language.Model;

namespace ImpliciX.Designer.ViewModels;

public class LivePercentagePropertyViewModel : LiveSingleDataViewModel
{
  public LivePercentagePropertyViewModel(
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
