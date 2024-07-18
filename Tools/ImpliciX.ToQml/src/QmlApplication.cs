using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ImpliciX.Language.GUI;
using ImpliciX.ToQml.Renderers;
using ImpliciX.ToQml.Renderers.Feeds;

namespace ImpliciX.ToQml;

public class QmlApplication
{
    public static GenerationResult Create(GuiDefinition definition, QmlRenderer qmlRenderer, string runtimeKind = "Client", string[] modules = null)
    {
        var result = new GenerationResult();
        
        var additionalModules = modules?.Select(m => $"import {m}").ToArray() ?? Array.Empty<string>();
        var rs = new RenderedScreens(definition, qmlRenderer, additionalModules);

        var keyboard = (definition.VirtualKeyboard ?? "implicix:implicix").Split(':');
        var (keyboardStyle, keyboardLayout) = (keyboard[0],keyboard[1]);

        var translationResourceName = definition.Assets.GetName().Name + "." + definition.Internationalization.TranslationFilename;
        var translations = new Translations(
            definition.Assets.GetManifestResourceStream(translationResourceName!)
        );

        result.AddRange(translations.Check(rs.Feeds));
        result.AddRange(translations.Check(definition.Screens.SelectMany(s => s.Value.Widgets)));
        
        var i18n = qmlRenderer.CreateContentFile(
            "translations.js",
            new SourceCodeGenerator()
                .Append(".pragma library")
                .Open("const data =")
                .CreateTranslationDictionary(translations)
                .Close()
                .CreateLocaleList()
                .CreateTimezoneList()
                .Result
        );

        var cache = qmlRenderer.CreateContentFile(
            "AppCache.qml",
            new SourceCodeGenerator()
                .Append("import QtQuick 2.13")
                .Append("import Runtime 1.0")
                .Append("import Shared 1.0")
                .Open("Cache")
                .Append($"locale : {qmlRenderer.FeedRenderers.GetValueOf(definition.Internationalization.Locale.InCache())}")
                .Append($"timeZone : {qmlRenderer.FeedRenderers.GetValueOf(definition.Internationalization.TimeZone.InCache())}")
                .Append(rs.Feeds.Select(f => qmlRenderer.FeedRenderers.Declare(f.InCache())).ToArray())
                .Close().Result
        );

        var runtime = qmlRenderer.CreateContentFile(
            "AppRuntime.qml",
            new SourceCodeGenerator()
                .Append($"import Runtime 1.0")
                .Append(additionalModules)
                .Open($"{runtimeKind}Runtime")
                .Close().Result
        );

        var app = qmlRenderer.CreateContentFile(
            "AppDefinition.qml",
            new SourceCodeGenerator()
                .Append("import QtQuick 2.13")
                .Open("Item")
                .Append($"readonly property int screen_width: {rs.ScreenSize.Width}")
                .Append($"readonly property int screen_height: {rs.ScreenSize.Height}")
                .Append($"readonly property string keyboard_style: '{keyboardStyle}'")
                .Append($"property string defaultPath: '{definition.StartupScreen.Urn.Value}'")
                .Append($"property bool hasScreenSaver: {(definition.ScreenSaver?.Screen != null).ToString().ToLower()}")
                .Append($"property bool hasScreenWhenNotConnected: {(definition.ScreenWhenNotConnected != null).ToString().ToLower()}")
                .Append($"property int screenSaverTimeout: {definition.ScreenSaver?.Timeout.TotalMilliseconds ?? -1}")
                .If(definition.ScreenSaver?.Screen != null, c => c.Append($"property string screenSaver: '{definition.ScreenSaver.Screen.Urn.Value}'"))
                .If(definition.ScreenWhenNotConnected != null, c =>
                    c.Append($"property string screenWhenNotConnected: '{definition.ScreenWhenNotConnected.Urn.Value}'")
                )
                .Open("property var routes:")
                .ForEach(rs.Screens, (s, scg) =>
                    scg
                        .Open($"'{s.Key}':")
                        .If(
                            rs.GroupOfScreen.TryGetValue(s.Key, out (GuiNode groupId, int index) x),
                            c => c
                                .Append($"file: '/{RenderedScreens.GetScreenName(x.groupId)}.qml',")
                                .Open("args:")
                                .Append($"position: {x.index}")
                                .Close(),
                            c => c.Append($"file: '/{s.Item2.qrc}'")
                        )
                        .Close("},")
                )
                .Close()
                .Close().Result
        );

        var resources = rs.Resources
            .Concat(new[] { i18n, cache, app, runtime })
            .Concat(rs.Screens.Select(s => s.Item2.qrc))
            .Concat(rs.Groups.Select(g => g.Item2)).ToArray();
        
        CreateQrc(resources).Save(Path.Combine(qmlRenderer.OutputFolder.FullName, "main.qrc"));

        qmlRenderer.CreateContentFile(
            "main.h",
            $"#define IMPLICIX_VIRTUALKEYBOARD_LAYOUT_PATH \":/{ChooseKeyboardLayoutPath(keyboardLayout, resources)}\""
        );

        return result;
    }

    private static XDocument CreateQrc(IEnumerable<string> resources)
    {
        var qrc = new XDocument(
            new XElement("RCC",
                new XElement("qresource",
                    new XAttribute("prefix", "/"),
                    resources.Select(x => new XElement("file", x))
                )
            )
        );

        return qrc;
    }

    public static string ChooseKeyboardLayoutPath(string keyboardLayout, string[] resources)
    {
        var keyboardLayoutPath =
            (from baseFolder in new[] { "LocalKeyboards", "Keyboards" }
                let path = $"{baseFolder}/QtQuick/VirtualKeyboard/Layout/{keyboardLayout}"
                from resource in resources
                where resource.StartsWith(path)
                select path).Distinct().FirstOrDefault();
        if (keyboardLayoutPath == null)
            throw new Exception($"Cannot find virtual keyboard layout {keyboardLayout}");
        return keyboardLayoutPath;
    }
}