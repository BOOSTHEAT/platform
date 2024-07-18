using DynamicData;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.DesignViewModels;

public class LiveFloatPropertyDataContext : LiveFloatPropertyViewModel
{
  private static readonly Urn RootUrn = Urn.BuildUrn("test");

  private static readonly Urn EntryUrn =
    UserSettingUrn<Mass>.Build(
      RootUrn,
      "Weight"
    );

  public LiveFloatPropertyDataContext() : base(
    EntryUrn,
    new LivePropertiesDataContext(),
    true
  )
  {
    LivePropertiesDataContext.SessionProperties.AddOrUpdate(
      new ImpliciXProperty(
        EntryUrn,
        "68"
      )
    );
  }
}
