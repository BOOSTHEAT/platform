using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Tests.Helpers;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests;

public class Sample1
{
  [Test]
  public void Test1()
  {
    Test(root.screen1);
  }

  [Test]
  public void Test2()
  {
    Test(root.screen2);
  }

  public void Test(GuiNode startup)
  {
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
        CreateMeteoPicto(),
        CreatePressurePicto(),
        new MyVerticalMenu(root.screen1, root.screen2)
      }
    };
  }

  private static Composite CreateMeteoPicto()
  {
    return new Composite
    {
      Left = 175, Top = 12,
      Content = new Widget[]
      {
        new ImageWidget { Path = Const.Is("assets/home/meteo.png"), IsBase = true },
        new Composite
        {
          Right = 14, Bottom = 8,
          Arrange = Composite.ArrangeAs.Row,
          Style = new Style { FontSize = 25, FrontColor = Colors.Orange },
          Content = new[]
          {
            new Text
            {
              Style = new Style { FontFamily = Style.Family.Medium },
              Value = MeasureFeed.Subscribe(production.heat_pump.external_unit.outdoor_temperature,
                MeasureFeed.UnitUsage.DoNotDisplayUnit)
            },
            new Text
            {
              Style = new Style { FontFamily = Style.Family.Light },
              Value = Const.Is("°C")
            }
          }
        },
      }
    };
  }
  
  private static Composite CreatePressurePicto()
  {
    var bar = new Text
    {
      Right = 6, Bottom = 6, Style = new Style
      {
        FontSize = 10,
        FrontColor = Colors.DarkGrey,
        FontFamily = Style.Family.Light
      },
      Value = Const.Is("bar")
    };
    var bold = new Style
    {
      FontSize = 22,
      FontFamily = Style.Family.ExtraBold
    };
    var pressureValue = MeasureFeed.Subscribe(production.main_circuit.supply_pressure, MeasureFeed.UnitUsage.DoNotDisplayUnit);
    return new Composite
    {
      Left = 35, Top = 259,
      Arrange = Composite.ArrangeAs.Column,
      Content = new Widget[]
      {
        new SwitchWidget
        {
          Cases = new[]
          {
            new SwitchWidget.Case
            {
              When = new LowerThan
              {
                Left = MeasureFeed.Subscribe(production.main_circuit.supply_pressure),
                Right = Const.Is(0.95)
              },
              Then = new Composite
              {
                Content = new Widget[]
                {
                  new ImageWidget { Path = Const.Is("assets/home/btn-pressure-low.png"), IsBase = true },
                  new Text
                  {
                    Right = 23, Bottom = 2, Style = bold.Override(frontColor: Colors.Orange), Value = pressureValue
                  },
                  bar
                }
              }
            }
          },
          Default = new Composite
          {
            Content = new Widget[]
            {
              new ImageWidget { Path = Const.Is("assets/home/btn-pressure-ok.png"), IsBase = true },
              new Text
              {
                Right = 23, Bottom = 2, Style = bold.Override(frontColor: Colors.DarkGrey), Value = pressureValue
              },
              bar
            }
          }
        },
        new Text
        {
          Style = new Style { FontFamily = Style.Family.Light, FontSize = 13 },
          Value = Const.IsTranslate("Water_Pressure"),
          Width = 62
        }
      }
    };
  }
}