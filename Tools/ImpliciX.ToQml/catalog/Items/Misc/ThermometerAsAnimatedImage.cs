using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public class ThermometerAsAnimatedImage : ItemBase
{
  public override string Title => "Animated Image";
  public override Block Display => Image("assets/thermometer.gif");
}