using ImpliciX.Designer.Features;
using ImpliciX.Designer.ViewModels;
using ImpliciX.DesktopServices;

namespace ImpliciX.Designer.DesignViewModels;

public class WelcomeViewDesignModel : WelcomeViewModel
{
  private const string BeaverKepAddress = "KepProxypel.boostheat.int";
  private const string MmiAddress = "bh06854821.boostheat.int";
  private const string NugetBeaverPath = "/home/charles/Téléchargements/Proxipel.Beaver.2024.2.13.1.nupkg";
  private const string NugetGimletPath = "/home/charles/Téléchargements/Demo.Gimlet.PlcSim.2024.2.13.1.nupkg";

  private const string CsprojGimletPath =
    "/home/charles/Téléchargements/ImpliciXDenonstrator/applications/Gimlet/Gimlet.App/src/Gimlet.App.csproj";

  private static readonly IFeatures Features = new DesignerFeatures();

  private static readonly ISessionService.Session Gimlet = new (
    NugetGimletPath,
    null
  );

  private static readonly ISessionService.Session LocalGimlet = new (
    CsprojGimletPath,
    null
  );

  private static readonly ISessionService.Session MMI = new (
    null,
    MmiAddress
  );

  private static readonly ISessionService.Session Beaver = new (
    NugetBeaverPath,
    BeaverKepAddress
  );

  private static readonly     ISessionService.Session[] DemoSessions =
  {
    Gimlet ,
    MMI,
    Beaver,
    LocalGimlet
  };

  public WelcomeViewDesignModel(
  ) : base(Features)
  {
    Sessions =
      DemoSessions;
  }
}
