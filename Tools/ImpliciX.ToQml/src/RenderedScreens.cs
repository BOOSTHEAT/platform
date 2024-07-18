using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers.Feeds;
using ImpliciX.ToQml.Renderers.Widgets;
using QrcItem = System.String;

namespace ImpliciX.ToQml;

public class RenderedScreens
{
    public string[] Resources { get; }
    public Size ScreenSize { get; }
    public (GuiNode Key, (string qrc, Feed[] feeds))[] Screens { get; }
    public (GuiNode Key, string)[] Groups { get; }
    public Dictionary<GuiNode, (GuiNode, int)> GroupOfScreen { get; }
    public Feed[] Feeds { get; }

    public RenderedScreens(GuiDefinition definition, QmlRenderer qmlRenderer, string[] additionalModules)
    {
        ScreenSize = definition.ScreenSize.IsEmpty
            ? new Size(800, 480)
            : definition.ScreenSize;

        Resources = ResourceManager.Load(qmlRenderer.OutputFolder, definition.Assets, qmlRenderer.CopyrightManager);

        Screens = (
            from screenNode in definition.Screens
            select (screenNode.Key, BuildScreen(qmlRenderer, additionalModules, screenNode.Key, screenNode.Value, ScreenSize))
        ).ToArray();
        Groups = (
            from groupNode in definition.ScreenGroups ?? new Dictionary<GuiNode, ScreenGroup>()
            select (groupNode.Key, BuildGroup(qmlRenderer, additionalModules, groupNode.Key, groupNode.Value, ScreenSize))
        ).ToArray();
        GroupOfScreen = (definition.ScreenGroups ?? new Dictionary<GuiNode, ScreenGroup>())
            .SelectMany(groupId => groupId.Value.Screens.Select((screenId, index) => (screenId, groupId.Key, index)))
            .ToDictionary(x => x.Item1, x => (x.Item2, x.Item3));

        Feeds = Screens
            .SelectMany(s => s.Item2.feeds)
            .Prepend(definition.Internationalization.TimeZone)
            .Prepend(definition.Internationalization.Locale)
            .Distinct(new FeedEqualityComparer(qmlRenderer.FeedRenderers))
            .Where(f => f != null)
            .ToArray();
    }

    private static string BuildGroup(QmlRenderer qmlRenderer, string[] additionalModules, GuiNode id, ScreenGroup group,
        Size size)
    {
        var code = new SourceCodeGenerator();
        code
            .Append("import QtQuick 2.13", "import QtQuick.Controls 2.13", "import Runtime 1.0", "import \".\"")
            .Append(additionalModules)
            .Open($"{group.Kind}Screen")
            .Append("id: root", $"name: '{GetScreenName(id)}'", $"width: {size.Width}", $"height: {size.Height}")
            .ForEach(group.Screens, (screenId, c) =>
            {
                c
                    .Open("SwipeItem")
                    .Open($"sourceComponent: {GetScreenName(screenId)}")
                    .Append("runtime: root.runtime")
                    .Close()
                    .Close();
            })
            .Close();
        var qmlFile = qmlRenderer.CreateContentFile(
            $"{GetScreenName(id)}.qml",
            code.Result
        );

        return qmlFile;
    }

    private static (string qrc, Feed[] feeds) BuildScreen(QmlRenderer qmlRenderer, string[] additionalModules,
        GuiNode id, Screen screen, Size size)
    {
        var feeds = screen.Widgets.SelectMany(w => qmlRenderer.WidgetRenderers.FindFeeds(w)).ToArray();
        var code = new SourceCodeGenerator();
        code
            .Append(
                "import QtQuick 2.13",
                "import QtQuick.Controls 2.13",
                "import Runtime 1.0",
                "import Shared 1.0",
                "import Widgets 1.0",
                "import QtQuick.VirtualKeyboard 2.13",
                "import QtCharts 2.13"
                )
            .Append(additionalModules)
            .Open("Rectangle")
            .Append("id: root", $"width: {size.Width}", $"height: {size.Height}", "property var runtime");

        const string absoluteRuntimePath = "root.runtime";
        foreach (var widget in screen.Widgets)
            qmlRenderer.WidgetRenderers.Render(new WidgetRenderingContext { Widget = widget, Code = code, Runtime = absoluteRuntimePath });
        code.Close();
        var qmlFile = qmlRenderer.CreateContentFile(
            $"{GetScreenName(id)}.qml",
            code.Result
        );

        return (qmlFile, feeds);
    }

    public static string GetScreenName(GuiNode id) => "Screen_" + id.Urn.Value.Replace(":", "__");
}