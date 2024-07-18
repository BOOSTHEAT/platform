using DynamicData;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.DesignViewModels;

public class LivePercentagePropertyDataContext : LivePercentagePropertyViewModel
{
  private static readonly Urn RootUrn = Urn.BuildUrn("test");

  private static readonly Urn EntryUrn =
    UserSettingUrn<Percentage>.Build(
      RootUrn,
      "progress"
    );

  public LivePercentagePropertyDataContext() : base(
    EntryUrn    ,
    new LivePropertiesDataContext(),
    true
  )
  {
    LivePropertiesDataContext.SessionProperties.AddOrUpdate(
      new ImpliciXProperty(
        EntryUrn,
        "0.5"
      )
    );
    NewValue = "0.5";
  }
}
