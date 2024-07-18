using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items.Inputs;

internal sealed class ScreenBlockBehaviors : Screens
{
  private static readonly string _rootUrn = nameof(ScreenBlockBehaviors).ToLower();
  public static readonly PropertyUrn<Pressure> pressure = PropertyUrn<Pressure>.Build(_rootUrn, "pressure");
  public static readonly PropertyUrn<Pressure> _pressure2 = PropertyUrn<Pressure>.Build(_rootUrn, "pressure2");
  public static CommandUrn<NoArg> write => CommandUrn<NoArg>.Build(_rootUrn, nameof(write));
  public static CommandUrn<NoArg> write2 => CommandUrn<NoArg>.Build(_rootUrn, nameof(write2));
  public static GuiNode Node => Category.CreateGuiNode(nameof(ScreenBlockBehaviors));

  public static AlignedBlock[] Blocks { get; } =
  {
    At.Top(10).Left(10)
      .Put(Label("Click me to multiple block behaviors").With(Font.Medium.Size(16))
        .Increment(pressure, 1.0)
        .Increment(_pressure2, 2.0)
        .Send(write)
        .Send(write2)
        .NavigateTo(ScreenGoBackToBlockBehaviors.Node, Box.Radius(16))
      ),
    At.Top(50).Left(10).Put(Show(pressure)),
    At.Top(70).Left(10).Put(Show(_pressure2)
    )
  };
}