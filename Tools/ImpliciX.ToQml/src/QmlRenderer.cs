using System;
using System.Collections.Generic;
using System.IO;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

namespace ImpliciX.ToQml;

public class QmlRenderer
{
  public readonly ICopyrightManager CopyrightManager;
  public DirectoryInfo OutputFolder { get; }
  public readonly Dictionary<Type, IRenderWidget> WidgetRenderers;
  public readonly Dictionary<Type, IRenderFeed> FeedRenderers;

  public QmlRenderer(DirectoryInfo outputFolder, ICopyrightManager copyrightManager)
  {
    CopyrightManager = copyrightManager;
    OutputFolder = outputFolder;
    FeedRenderers = new Dictionary<Type, IRenderFeed>
    {
      [typeof(Const)] = new ConstRenderer(),
      [typeof(NowFeed)] = new NowRenderer(),
      [typeof(PropertyFeed)] = new PropertyRenderer(),
      [typeof(TimeSeriesFeed)] = new TimeSeriesRenderer(),
      [typeof(MeasureFeed)] = new MeasureRenderer()
    };

    FeedRenderers[typeof(LowerThan)] = new LowerThanRenderer(FeedRenderers);
    FeedRenderers[typeof(GreaterThan)] = new GreaterThanRenderer(FeedRenderers);
    FeedRenderers[typeof(EqualTo)] = new EqualToRenderer(FeedRenderers);
    FeedRenderers[typeof(NotEqualTo)] = new NotEqualToRenderer(FeedRenderers);

    WidgetRenderers = new Dictionary<Type, IRenderWidget>();
    WidgetRenderers[typeof(Composite)] = new CompositeRenderer(WidgetRenderers);
    WidgetRenderers[typeof(SwitchWidget)] = new SwitchRenderer(WidgetRenderers, FeedRenderers);
    WidgetRenderers[typeof(ImageWidget)] = new ImageRenderer(outputFolder, FeedRenderers);
    WidgetRenderers[typeof(DataDrivenImageWidget)] = new DataDrivenImageRenderer(outputFolder, FeedRenderers);
    WidgetRenderers[typeof(Text)] = new TextRenderer(FeedRenderers);
    WidgetRenderers[typeof(TextBox)] = new TextBoxRenderer(FeedRenderers);
    WidgetRenderers[typeof(NavigatorWidget)] = new NavigatorRenderer(WidgetRenderers);
    WidgetRenderers[typeof(BoxWidget)] = new BoxRenderer();
    WidgetRenderers[typeof(BarsWidget)] = new BarsChartRenderer(FeedRenderers);
    WidgetRenderers[typeof(PieChartWidget)] = new PieChartRenderer(FeedRenderers);
    WidgetRenderers[typeof(StackedTimeBarsWidget)] = new StackedTimeBarsRenderer(FeedRenderers);
    WidgetRenderers[typeof(TimeLinesWidget)] = new TimeLinesRenderer(FeedRenderers);
    WidgetRenderers[typeof(MultiChartWidget)] = new MultiChartRenderer(FeedRenderers);
    WidgetRenderers[typeof(OnOffButtonWidget)] = new OnOffButtonRender(FeedRenderers);
    WidgetRenderers[typeof(DropDownListWidget)] = new DropDownListRender(FeedRenderers);
    WidgetRenderers[typeof(IncrementWidget)] = new IncrementRenderer(WidgetRenderers, FeedRenderers);
    WidgetRenderers[typeof(SendWidget)] = new SendRenderer(WidgetRenderers);
  }

  public void AddRenderer<T>(IRenderWidget renderer) => WidgetRenderers[typeof(T)] = renderer;

  public string CreateContentFile(string filename, string content)
  {
    content = CopyrightManager.AddCopyright(content, filename);
    File.WriteAllText(
      Path.Combine(OutputFolder.FullName, filename),
      content
    );

    return filename;
  }
}