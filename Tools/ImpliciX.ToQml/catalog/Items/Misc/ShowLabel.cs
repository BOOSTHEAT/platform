using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Catalog.Items;


public class ShowLabel : ItemBase
{
  public override string Title => "Show label";

  public override Block Display => Column.Layout(
    Label(Text).Width(400),
    Label(Text).Width(300),
    Label(Text).Width(200),
    Label(Text).Width(100),
    Label(Text).Width(50)
    );

  private const string Text = "The quick brown fox jumps over the lazy dog";
}
