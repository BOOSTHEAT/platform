using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml.Tests;

public class MyVerticalMenu : Composite
{
  public MyVerticalMenu(GuiNode homeScreen, GuiNode dhwScreen)
  {
    var onTarget = new BoxWidget
    {
      Style = new Style { FrontColor = Colors.DarkGrey2 },
      Radius = 12
    };
    Top = 0;
    Right = 0;
    Content = new Widget[]
    {
      new BoxWidget
      {
        Width = 84,
        Height = 480,
        IsBase = true,
        Style = new Style { BackColor = Colors.LighterGrey }
      },
      new Composite
      {
        HorizontalCenterOffset = 0,
        VerticalCenterOffset = 0,
        Arrange = ArrangeAs.Column,
        Spacing = 16,
        Content = new Widget[]
        {
          new NavigatorWidget
          {
            Visual = new ImageWidget
            {
              Path = Const.Is("assets/menu/home.png")
            },
            TargetScreen = homeScreen,
            OnTarget = onTarget
          },
          new NavigatorWidget
          {
            Visual = new ImageWidget
            {
              Path = Const.Is("assets/menu/shower_eco_running.gif")
            },
            TargetScreen = dhwScreen,
            OnTarget = onTarget
          }
        }
      }
    };
  }
}