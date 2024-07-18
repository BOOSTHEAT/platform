using System.Reactive.Subjects;
using DynamicData;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Designer.DesignViewModels;

public class LiveFunctionDefinitionDataContext : LiveFunctionDefinitionViewModel
{
  private static readonly string ContextTitle = "DataContext";

  private static readonly ILightConcierge
    Concierge = IConcierge.Create(new User(ContextTitle)); // new LightConcierge();

  internal static readonly SourceCache<ImpliciXProperty, string> SessionProperties = new (x => x.Urn);
  private static readonly Subject<Option<IDeviceDefinition>> Devices = new ();

  private static readonly Urn RootUrn = Urn.BuildUrn("test");

  private static readonly Urn EntryUrn =
    UserSettingUrn<FunctionDefinition>.Build(
      RootUrn,
      "Function"
    );

  public LiveFunctionDefinitionDataContext(
  ) : base(
    EntryUrn,
    new LivePropertiesDataContext(),
    true
  )
  {
    LivePropertiesDataContext.SessionProperties.AddOrUpdate(
      new ImpliciXProperty(
        EntryUrn,
        "a10:1|a01:1.1"
      )
    );
  }
}
