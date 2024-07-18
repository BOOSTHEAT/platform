using DynamicData;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.DesignViewModels;

public class LiveEnumPropertyDataContext : LiveEnumPropertyViewModel
{
  private static readonly Urn RootUrn = Urn.BuildUrn("test");

  private static readonly Urn EntryUrn =
    UserSettingUrn<UpdateState>.Build(
      RootUrn,
      "Status"
    );

  public LiveEnumPropertyDataContext(  ) : base(
    EntryUrn,
    new LivePropertiesDataContext(),
    true
  )
  {
    LivePropertiesDataContext.SessionProperties.AddOrUpdate(
      new ImpliciXProperty(
        EntryUrn,
        "Ready"
      )
    );
  }
}
