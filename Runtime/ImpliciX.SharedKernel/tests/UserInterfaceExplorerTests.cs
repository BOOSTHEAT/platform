using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Tools;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests;

public class UserInterfaceExplorerTests : Screens
{
  [Test]
  public void FindAllWidgets()
  {
    var widgets = UserInterfaceExplorer.GetInformationFor(Definition).Widgets;
    Assert.That( widgets.Select(AsString), Is.EqualTo(new []
    {
      "Switch",
      "Image assets/state/running.gif",
      "Image assets/state/stopped.png",
      "Composite Column",
      "Navigate root:screen1",
      "Image assets/menu/main.png",
      "Box",
      "Navigate root:screen2",
      "Image assets/menu/details.png",
      "Box",
      "Text Now",
      
      "Composite Row",
      "Text Pump State: ",
      "Text root:property1",
      "TimeLines",
      "StackedTimeBars",
      "Composite Column",
      "Navigate root:screen1",
      "Image assets/menu/main.png",
      "Box",
      "Navigate root:screen2",
      "Image assets/menu/details.png",
      "Box",
      "Text Now",
    }) );
  }
  
  [Test]
  public void FindChartXSpans()
  {
    var xSpans = UserInterfaceExplorer.GetInformationFor(Definition).XSpans;
    Assert.That( xSpans, Is.EqualTo(new (Urn,ChartXTimeSpan)[]
    {
      (Urn.BuildUrn("root","metric1"), new ChartXTimeSpan(6, TimeUnit.Months)),
      (Urn.BuildUrn("root","metric2"), new ChartXTimeSpan(6, TimeUnit.Months)),
      (Urn.BuildUrn("root","metric1"), new ChartXTimeSpan(1, TimeUnit.Years)),
    }) );
  }

  public static string AsString(Widget w) => w switch
  {
    SwitchWidget _ => "Switch",
    ImageWidget iw => $"Image {AsString(iw.Path)}",
    Text tw => $"Text {AsString(tw.Value)}",
    Composite cw => $"Composite {cw.Arrange}",
    NavigatorWidget nw => $"Navigate {nw.TargetScreen.Urn.Value}",
    BoxWidget _ => "Box",
    TimeLinesWidget _ => "TimeLines",
    StackedTimeBarsWidget _ => "StackedTimeBars",
    _ => w.ToString()
  };
  
  public static string AsString(Feed f) => f switch
  {
    Const<string> cf => cf.Value,
    NowFeed _ => "Now",
    Node nf => nf.Urn.Value,
    _ => f.ToString()
  };

  public static GUI Definition()
  {
    var root = new RootModelNode("root");
    var screen1 = new GuiNode(root,"screen1");
    var screen2 = new GuiNode(root,"screen2");
    var property1 = PropertyUrn<Fault>.Build("root:property1");
    var metric1 = PropertyUrn<MetricValue>.Build("root:metric1");
    var metric2 = PropertyUrn<MetricValue>.Build("root:metric2");
    
    var selected = Box.Radius(12);

    var menu = At.Right(40).Top(50).Put(
      Column.Spacing(30).Layout(
        Image("assets/menu/main.png").NavigateTo(screen1,selected),
        Image("assets/menu/details.png").NavigateTo(screen2,selected)
      ));
  
    var time = At.Right(10).Bottom(10)
      .Put(Now.HoursMinutesSeconds.With(Font.Light.Size(26)));

    return GUI
      // .Assets(Assembly.GetExecutingAssembly())
      .StartWith(screen1)
      
      .Screen(screen1, Screen(
        At.Left(50).Top(18).Put(
          Switch
            .Case(Value(property1) == Fault.NotFaulted,
              Image("assets/state/running.gif")
            )
            .Default(Image("assets/state/stopped.png"))
        ),
        menu,
        time
      ))
      
      .Screen(screen2, Screen(
        At.Left(20).Top(110).Put(
          Row.Layout(
            Label("Pump State: ").With(Font.Light),
            Show(property1).With(Font.Medium)
          ).With(Font.Size(25))
        ),
        At.Left(20).Top(110).Put(
          Chart.TimeLines(
            Of(metric1),
            Of(metric2)
          ).Over.ThePast(6).Months
        ),
        At.Left(20).Top(110).Put(
          Chart.StackedTimeBars(
            Of(metric1)
          ).Over.ThePast(1).Years
        ),
        menu,
        time
      ));
  }

}