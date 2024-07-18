using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Catalog.Items.Inputs;

internal sealed class ScreenGoBackToBlockBehaviors : Screens
{
  public static GuiNode Node => Category.CreateGuiNode(nameof(ScreenGoBackToBlockBehaviors));
  private static GuiNode TargetNode => Category.CreateGuiNode(CategoryName.Inputs, nameof(BlockBehaviorsItem));

  public static AlignedBlock[] Blocks { get; } =
  {
    At.Origin.Put(Box.Width(CatalogGui.ScreenWidth).Height(CatalogGui.ScreenWidth)),
    At.HorizontalCenterOffset(0).VerticalCenterOffset(0)
      .Put(
        Label($"Click to go back to {nameof(ScreenBlockBehaviors)}")
          .With(Font.ExtraBold.Size(48))
          .NavigateTo(TargetNode, Canvas.Layout(ScreenBlockBehaviors.Blocks))
      )
  };
}