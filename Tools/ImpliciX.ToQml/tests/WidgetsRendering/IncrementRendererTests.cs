using System;
using System.Collections.Generic;
using System.Globalization;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ToQml.Tests.WidgetsRendering;

public class IncrementRendererTests : Screens
{
  [Test]
  public void GivenSingleShow_WhenIRender()
  {
    var temperatureThreshold = PropertyUrn<Temperature>.Build("production", "heating", "temperature_threshold");
    var userSettingUrn = UserSettingUrn<IFloat>.Build("system", "settings", "temperature_threshold");
    var blockWidget = At.Left(50).Top(50).Put(
        Show(temperatureThreshold)
          .Increment(userSettingUrn, 1)
      )
      .CreateWidget();

    var context = new WidgetRenderingContext
    {
      Widget = blockWidget,
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(PropertyFeed)] = new PropertyRenderer()};
    var widgetRenderers = new Dictionary<Type, IRenderWidget> {[typeof(Text)] = new TextRenderer(feedRenderers)};
    new IncrementRenderer(widgetRenderers, feedRenderers).Render(context);

    Check.That(context.Code.Result).IsEqualTo(@" ClickableContainer {
  width:visual.width
  height:visual.height
  anchors {
    left: parent.left
    leftMargin: 50
    top: parent.top
    topMargin: 50
  }
  visual: Text {
    text: root.runtime.cache.production$heating$temperature_threshold.display()
  }
  onClicked: {
    var newValue = Js.toFloat(root.runtime.cache.system$settings$temperature_threshold.value) + 1;
    root.runtime.api.sendProperty(""system:settings:temperature_threshold"", newValue);
    if(typeof parent.clicked === 'function') parent.clicked();
  }
}
", StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.None)
    );
  }

  [Test]
  public void GivenSingleBox_WhenIRender()
  {
    var userSettingUrn = UserSettingUrn<IFloat>.Build("system", "settings", "temperature_threshold");
    var blockWidget = At.Left(50).Top(50).Put(
        Box.Radius(16).Height(40).Width(40)
          .Increment(userSettingUrn, 1)
      )
      .CreateWidget();

    var context = new WidgetRenderingContext
    {
      Widget = blockWidget,
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(PropertyFeed)] = new PropertyRenderer()};
    var widgetRenderers = new Dictionary<Type, IRenderWidget> {[typeof(BoxWidget)] = new BoxRenderer()};
    new IncrementRenderer(widgetRenderers, feedRenderers).Render(context);

    Check.That(context.Code.Result).IsEqualTo(@" ClickableContainer {
  width:visual.width
  height:visual.height
  anchors {
    left: parent.left
    leftMargin: 50
    top: parent.top
    topMargin: 50
  }
  visual: Rectangle {
    width: 40
    height: 40
    radius: 16
    color : 'transparent'
  }
  onClicked: {
    var newValue = Js.toFloat(root.runtime.cache.system$settings$temperature_threshold.value) + 1;
    root.runtime.api.sendProperty(""system:settings:temperature_threshold"", newValue);
    if(typeof parent.clicked === 'function') parent.clicked();
  }
}
", StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.None)
    );
  }

  [Test]
  public void GivenSingleChart_WhenIRender()
  {
    var temperature1 = PropertyUrn<Temperature>.Build("production", "heating", "temperature");
    var temperature2 = PropertyUrn<Temperature>.Build("production", "heating", "temperature");
    var userSettingUrn = UserSettingUrn<IFloat>.Build("system", "settings", "temperature_threshold");

    var blockWidget = At.Left(50).Top(50).Put(
      Chart.Pie(
        Of(temperature1),
        Of(temperature2)
      ).Increment(userSettingUrn, 1)
    ).CreateWidget();

    var context = new WidgetRenderingContext
    {
      Widget = blockWidget,
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed> {[typeof(PropertyFeed)] = new PropertyRenderer()};
    var widgetRenderers = new Dictionary<Type, IRenderWidget> {[typeof(PieChartWidget)] = new PieChartRenderer(feedRenderers)};
    new IncrementRenderer(widgetRenderers, feedRenderers).Render(context);

    Check.That(context.Code.Result).IsEqualTo(@" ClickableContainer {
  width:visual.width
  height:visual.height
  anchors {
    left: parent.left
    leftMargin: 50
    top: parent.top
    topMargin: 50
  }
  visual:ChartView {
    anchors {
      left: parent.left
      leftMargin: 0
      top: parent.top
      topMargin: 0
      right: parent.right
      rightMargin: 0
      bottom: parent.bottom
      bottomMargin: 0
    }
    legend.visible: false
    antialiasing: true
    MouseArea {
    anchors.fill: parent
    onClicked: parent.parent.clicked()
  }
    PieSeries {
      holeSize: 0.5
      PieSlice {
        value: root.runtime.cache.production$heating$temperature.value
        label: ""%1%"".arg((100 * percentage).toFixed(1))
        labelArmLengthFactor: 0
        labelVisible: true
        labelFont:Qt.font({family:UiConst.fontBtr,pixelSize:12})
      }
      PieSlice {
        value: root.runtime.cache.production$heating$temperature.value
        label: ""%1%"".arg((100 * percentage).toFixed(1))
        labelArmLengthFactor: 0
        labelVisible: true
        labelFont:Qt.font({family:UiConst.fontBtr,pixelSize:12})
      }
    }
  }
  onClicked: {
    var newValue = Js.toFloat(root.runtime.cache.system$settings$temperature_threshold.value) + 1;
    root.runtime.api.sendProperty(""system:settings:temperature_threshold"", newValue);
    if(typeof parent.clicked === 'function') parent.clicked();
  }
}
", StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.None)
    );
  }

  [Test]
  public void GivenCanvasSingleRow_WhenIRender()
  {
    var userSettingUrn = UserSettingUrn<IFloat>.Build("system", "settings", "temperature_threshold");
    var blockWidget = Canvas.Layout(
        At.Origin.Put(
          Row.Layout(
            Label("mon label1"),
            Label("mon label2")
          )
        )
      ).Increment(userSettingUrn, 1)
      .CreateWidget();

    var context = new WidgetRenderingContext
    {
      Widget = blockWidget,
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed>
    {
      [typeof(Const)] = new ConstRenderer(),
      [typeof(PropertyFeed)] = new PropertyRenderer()
    };

    var widgetRenderers = new Dictionary<Type, IRenderWidget> {[typeof(Text)] = new TextRenderer(feedRenderers)};
    widgetRenderers[typeof(Composite)] = new CompositeRenderer(widgetRenderers);

    new IncrementRenderer(widgetRenderers, feedRenderers).Render(context);

    Check.That(context.Code.Result).IsEqualTo(@" ClickableContainer {
  width:visual.width
  height:visual.height
  visual: Rectangle {
    anchors {
      left: parent.left
      leftMargin: 0
      top: parent.top
      topMargin: 0
      right: parent.right
      rightMargin: 0
      bottom: parent.bottom
      bottomMargin: 0
    }
    property var item0: Row {
      anchors {
        left: parent.left
        leftMargin: 0
        top: parent.top
        topMargin: 0
      }
      property var item0: Text {
        text: ""mon label1""
      }
      property var item1: Text {
        text: ""mon label2""
      }
      data: [item0,item1]
      spacing: 0
    }
    data: [item0]
  }
  onClicked: {
    var newValue = Js.toFloat(root.runtime.cache.system$settings$temperature_threshold.value) + 1;
    root.runtime.api.sendProperty(""system:settings:temperature_threshold"", newValue);
    if(typeof parent.clicked === 'function') parent.clicked();
  }
}
", StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.None)
    );
  }

  [Test]
  public void GivenCompositeWithTwoRow_WhenIRender()
  {
    var userSettingUrn = UserSettingUrn<IFloat>.Build("system", "settings", "temperature_threshold");
    var blockWidget = Canvas.Layout(
      At.Origin.Put(
        Column.Layout(
          Row.Layout(
            Label("row 10"),
            Label("row 20")
          ).Increment(userSettingUrn, 1.0),
          Row.Layout(
            Label("row 1"),
            Label("row 2")
          ).Increment(userSettingUrn, -1.0)
        )
      )
    ).CreateWidget();

    var context = new WidgetRenderingContext
    {
      Widget = blockWidget,
      Code = new SourceCodeGenerator(),
      Runtime = "root.runtime"
    };

    var feedRenderers = new Dictionary<Type, IRenderFeed>
    {
      [typeof(Const)] = new ConstRenderer(),
      [typeof(PropertyFeed)] = new PropertyRenderer()
    };

    var widgetRenderers = new Dictionary<Type, IRenderWidget> {[typeof(Text)] = new TextRenderer(feedRenderers)};
    widgetRenderers[typeof(Composite)] = new CompositeRenderer(widgetRenderers);
    widgetRenderers[typeof(IncrementWidget)] = new IncrementRenderer(widgetRenderers, feedRenderers);
    new CompositeRenderer(widgetRenderers).Render(context);

    Check.That(context.Code.Result).IsEqualTo(@" Rectangle {
  anchors {
    left: parent.left
    leftMargin: 0
    top: parent.top
    topMargin: 0
    right: parent.right
    rightMargin: 0
    bottom: parent.bottom
    bottomMargin: 0
  }
  property var item0: Column {
    anchors {
      left: parent.left
      leftMargin: 0
      top: parent.top
      topMargin: 0
    }
    property var item0: ClickableContainer {
      width:visual.width
      height:visual.height
      visual: Row {
        property var item0: Text {
          text: ""row 10""
        }
        property var item1: Text {
          text: ""row 20""
        }
        data: [item0,item1]
        spacing: 0
      }
      onClicked: {
        var newValue = Js.toFloat(root.runtime.cache.system$settings$temperature_threshold.value) + 1;
        root.runtime.api.sendProperty(""system:settings:temperature_threshold"", newValue);
        if(typeof parent.clicked === 'function') parent.clicked();
      }
    }
    property var item1: ClickableContainer {
      width:visual.width
      height:visual.height
      visual: Row {
        property var item0: Text {
          text: ""row 1""
        }
        property var item1: Text {
          text: ""row 2""
        }
        data: [item0,item1]
        spacing: 0
      }
      onClicked: {
        var newValue = Js.toFloat(root.runtime.cache.system$settings$temperature_threshold.value) + -1;
        root.runtime.api.sendProperty(""system:settings:temperature_threshold"", newValue);
        if(typeof parent.clicked === 'function') parent.clicked();
      }
    }
    data: [item0,item1]
    spacing: 0
  }
  data: [item0]
}
", StringComparer.Create(CultureInfo.InvariantCulture, CompareOptions.None)
    );
  }
}