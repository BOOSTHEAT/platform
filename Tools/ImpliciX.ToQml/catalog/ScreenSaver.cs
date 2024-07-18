using System.Drawing;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Catalog;

public class ScreenSaver : Screens
{
  public static GuiNode Node { get; } = Category.CreateGuiNode("screen_saver");

  public static AlignedBlock[] Blocks { get; } = new[]
  {
    At.Origin.Put(Box.Width(CatalogGui.ScreenWidth).Height(CatalogGui.ScreenWidth).Fill(Color.Black)),
    At.Left(10).Top(10).Put( Label("Saving the screen...").With(Font.ExtraBold.Size(48).Color(Color.Beige)))
  };
}