using System;
using DynamicData;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.DesignViewModels;

public class LiveTextPropertyDataContext : LiveTextPropertyViewModel
{
  private static readonly Urn RootUrn = Urn.BuildUrn("test");

  private static readonly Urn EntryUrn =
    UserSettingUrn<Guid>.Build(
      RootUrn,
      "Guid"
    );

  public LiveTextPropertyDataContext() : base(
    EntryUrn,
    new LivePropertiesDataContext(),
    true
  )
  {
    LivePropertiesDataContext.SessionProperties.AddOrUpdate(
      new ImpliciXProperty(
        EntryUrn,
        "test GUID"
      )
    );
  }
}
