using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Tests.Helpers;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

public class Sample2
{
  [Test]
  public void Test()
  {
    var startup = root.screen1;
    var gui = new GuiDefinition
    {
      Screens = new Dictionary<GuiNode, Screen>
      {
        [root.screen1] = CreateScreen(new Composite
        {
          Left = 166, Top = 41, Content = new[]
          {
            new ImageWidget { Path = Const.Is("assets/home/floor2.png") },
            new ImageWidget { Path = Const.Is("assets/home/home_isometric-wall.png") },
            new ImageWidget { Path = Const.Is("assets/home/floor1.png") },
            new ImageWidget { Path = Const.Is("assets/home/home_isometric-EU-off.png") },
          }
        }),
        [root.screen2] = CreateScreen(
          new Composite
          {
            Left = 166, Top = 41, Content = new[]
            {
              new ImageWidget { Path = Const.Is("assets/home/home_isometric-heater_2.png") },
              new ImageWidget { Path = Const.Is("assets/home/home_isometric-wall.png") },
              new ImageWidget { Path = Const.Is("assets/home/home_isometric-heater.png") },
              new ImageWidget { Path = Const.Is("assets/home/home_isometric-EU-off.png") },
            }
          })
      },
      StartupScreen = startup,
      Assets = Assembly.GetExecutingAssembly(),
      Internationalization = new Internationalization
      {
        Language = PropertyFeed.Subscribe(general.users._1.language),
        TranslationFilename = "translations.csv",
        Locale = PropertyFeed.Subscribe(general.users._1.locale),
        TimeZone = PropertyFeed.Subscribe(general.users._1.timezone)
      }
    };
    var folder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "QML_" + Path.GetRandomFileName()));
    folder.Create();
    var rendering = new QmlRenderer(folder,new NullCopyrightManager());
    QmlApplication.Create(gui, rendering);
  }
  
  private static Screen CreateScreen(Composite composite)
  {
    var date = new Composite
    {
      Left = 14, Top = 10,
      Arrange = Composite.ArrangeAs.Column,
      Style = new Style { FontSize = 26, FrontColor = Colors.DarkGrey, FontFamily = Style.Family.Light },
      Content = new[]
      {
        new Text { Value = NowFeed.WeekDay },
        new Text { Value = NowFeed.Date },
        new Text { Value = NowFeed.HoursMinutesSeconds }
      }
    };
    var background = new Composite
    {
      Left = 166, Top = 41, Content = new[]
      {
        new ImageWidget { Path = Const.Is("assets/home/home_isometric-shower.png") },
        new ImageWidget { Path = Const.Is("assets/home/home_isometric-empty.png") },
        new ImageWidget { Path = Const.Is("assets/home/home_isometric-bh20.png") },
      }
    };
    return new Screen
    {
      Widgets = new Widget[]
      {
        new Text { Right = 100, Top = 10, Value = Const.Is("Hello world") },
        date,
        background,
        composite,
        new MyVerticalMenu(root.screen1, root.screen2)
      }
    };
  }
}