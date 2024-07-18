using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Helpers;

namespace ImpliciX.ToQml.Renderers.Widgets.TimeCharts;

public class TimeChartCodeGeneration
{
  public TimeChartCodeGeneration(WidgetRenderingContext context)
  {
    _context = context;
  }

  private readonly WidgetRenderingContext _context;
  public string Cache => _context.Cache;
  public SourceCodeGenerator Code => _context.Code;
  public readonly Style DefaultStyle = new() { FontFamily = Style.Family.Medium, FontSize = 11 };

  public void UseTimeLines()
  {
    Code
      .Open("function updateLineSeries(lineSeries, timeSeries)")
      .Append("lineSeries.removePoints(0, lineSeries.count);")
      .Open("for(var i=0; i<timeSeries.values.length; i++)")
      .Append("lineSeries.append(timeSeries.timeline[i], timeSeries.values[i]);")
      .Close()
      .Close();
  }

  public void OpenChartView()
  {
    Code
      .Open(_context.Prefix + "ChartView")
      .Open($"onSeriesAdded:")
      .Open("if(series.xAxisType == 0)")
      .Append($"series.axisX = xMainAxis;")
      .Close()
      .Open("else")
      .Append($"series.axisX = xSecondaryAxis;")
      .Close()
      .Open("if(series.yAxisPosition == 0)")
      .Append($"series.axisY = yLeftAxis;")
      .Close()
      .Open("else")
      .Append($"series.axisYRight = yRightAxis;")
      .Close()
      .Close();

    _context.RenderBase(useParentSizeIfSizeSettingMissing: true);

    Code
      .Append("legend.visible: false")
      .Append("antialiasing: true")
      .Append(_context.OnClickedEventRoute.IsSome,
        () => ChartRenderHelpers.GetMouseAreaForOnClickedEventRoute(_context.OnClickedEventRoute.GetValue()));
  }
  
  public void CloseChartView()
  {
    Code.Close();
  }
  
  public void DefineXAxisFromCategories(string timeLine, bool isSecondary = false)
  {
    if (isSecondary)
    {
      Code
        .Open($"property var xSecondaryAxis : DateTimeAxis")
        .Append($"min: {timeLine}.getTimelineMinDate(-{timeLine}.getHalfPeriod())")
        .Append($"max: {timeLine}.getTimelineMaxDate({timeLine}.getHalfPeriod())")
        .Append("visible: false")
        .Close();
    }
    else
    {
      Code
        .Open("property var xMainAxis: BarCategoryAxis")
        .Append($"categories: {timeLine}.getFormattedTimeline({timeLine}.determineTimelineFormat())")
        .Append($"labelsFont:{DefaultStyle.AsQtFont()}")
        .Close();
    }
  }

  public void DefineXAxisFromDateRange(string timeLine)
  {
    Code
      .Open($"property var xMainAxis : DateTimeAxis")
      .Append($"min: {timeLine}.getTimelineMinDate()")
      .Append($"max: {timeLine}.getTimelineMaxDate()")
      .Append($"format: {timeLine}.determineTimelineFormat()")
      .Append($"titleText: \"(\" + {timeLine}.determineTimelineFormat() + \")\"")
      .Append($"labelsFont:{DefaultStyle.AsQtFont()}")
      .Close();
  }
  
  public enum VerticalAxisPosition
  {
    Left,
    Right
  };
  
  public void DefineYAxis(
    TimeChartFeeds feeds,
    VerticalAxisPosition position,
    string yMin,
    string yMax)
  {
    Code
      .Open($"property var y{position}Axis: ValueAxis")
      .Append(yMin)
      .Append(yMax)
      .Append($"labelsFont:{DefaultStyle.AsQtFont()}")
      .Close();
  }
  
  public void OpenSeries(string type, VerticalAxisPosition position, bool isSecondary = false)
  {
    Code
      .Open(type)
      .Append($"property var xAxisType : {(isSecondary ? 1 : 0)}")
      .Append($"property var yAxisPosition : {(int)position}");
  }

  public void CloseSeries()
  {
    Code.Close();
  }
  
  public void AddBarSet(string label, string values, Color? barFillColor)
  {
    Code
      .Open("BarSet")
      .Append($@"label: ""{label}""")
      .Append($"values: {values}")
      .Append(barFillColor.HasValue, () => $"color: {StyleRenderer.ToQmlString(barFillColor!.Value)}")
      .Close();
  }
  
  public void AddLine(string timeSeries, string values, Color? lineColor)
  {
    Code
      .Append($"property var tsValues : {values}")
      .Open($"onTsValuesChanged:")
      .Append($"updateLineSeries(this, {timeSeries});")
      .Close()
      .Append(lineColor.HasValue, () => $@"color: {StyleRenderer.ToQmlString(lineColor!.Value)}");
  }
}