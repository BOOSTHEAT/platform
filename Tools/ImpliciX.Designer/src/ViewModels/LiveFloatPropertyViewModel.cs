using System.Reactive.Linq;
using ImpliciX.Language.Model;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public class LiveFloatPropertyViewModel : LiveSingleDataViewModel
{
  private readonly ObservableAsPropertyHelper<bool> _isValidated;

  public LiveFloatPropertyViewModel(
    Urn urn,
    LivePropertiesViewModel parent,
    bool inModel
  ) : base(
    urn,
    parent,
    inModel
  )
  {
    this
      .WhenAnyValue(model => model.NewValue )
      .Select(s => !string.IsNullOrEmpty(s) )
      .ToProperty(
        this,
        nameof(IsValidated),
        out _isValidated
      );
  }

  public override bool IsValidated => _isValidated.Value;
}
