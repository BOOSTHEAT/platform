using System.Drawing;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Catalog;

public class WaitingConnectionScreen : Screens
{
  public static GuiNode Node { get; } = Category.CreateGuiNode("waiting_connection");

  public static AlignedBlock[] Blocks { get; } = new[]
  {
    At.Origin.Put(Box.Width(CatalogGui.ScreenWidth).Height(CatalogGui.ScreenWidth).Fill(Color.Red)),
    At.Left(10).Top(10).Put( Label("Waiting for backend connection...").With(Font.ExtraBold.Size(48).Color(Color.Gold)))
  };
}