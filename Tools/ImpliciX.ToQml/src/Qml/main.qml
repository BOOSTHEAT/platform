import QtQuick 2.13
import QtQuick.Window 2.13
import QtQuick.Controls 2.13  // for stackView
import QtQuick.VirtualKeyboard 2.13
import QtQuick.VirtualKeyboard.Styles 2.13
import QtQuick.VirtualKeyboard.Settings 2.13
import QtQml 2.5
import Shared 1.0
import Runtime 1.0
import "translations.js" as Translations

Window {
    visible: true
    
    readonly property int screen_width: app.screen_width
    readonly property int screen_height: app.screen_height
    
    width: screen_width
    height: screen_height
    maximumWidth: screen_width
    maximumHeight: screen_height
    minimumWidth: screen_width
    minimumHeight: screen_height
    
    title: qsTr("ImpliciX GUI")

    id: root_window
    
    MouseArea {
        id: mouse_area
        anchors.fill: parent
        onClicked: screenSaver.notifyUserAction()

        StackView {
            id: stack
            anchors.fill: parent
        }
    }
    InputPanel {
        id: inputPanel
        anchors.left: parent.left
        anchors.right: parent.right
        y: Qt.inputMethod.visible ? parent.height - inputPanel.height : parent.height
        Component.onCompleted: {
            VirtualKeyboardSettings.fullScreenMode = true;
            VirtualKeyboardSettings.styleName = app.keyboard_style;
        }
    }
    
    function updateLocalization() {
      if(runtime.cache.locale && runtime.cache.localeList)
        VirtualKeyboardSettings.locale =  runtime.cache.localeList[runtime.cache.locale];
    }


        
    AppDefinition {
        id: app
    }
    
    Router {
        id: router
        stackView: stack
        runtime: runtime
        defaultPath: app.defaultPath
        routes: app.routes
    }
    
    ScreenSaver {
        id: screenSaver
        isPresent: app.hasScreenSaver && !Qt.inputMethod.visible && !waitConnection.isActive
        timeout: app.screenSaverTimeout
        onEntered: runtime.gotoSpecialScreen(app.screenSaver)
        onLeave: runtime.goBackToStandardScreen()
    }
    
    WaitConnectionScreen {
        id: waitConnection
        isPresent: app.hasScreenWhenNotConnected
        runtime: runtime
        onEntered: runtime.gotoSpecialScreen(app.screenWhenNotConnected)
        onLeave: runtime.goBackToStandardScreen()
    }
    

    AppRuntime {
        id: runtime
        cache: AppCache {
            translations: Translations.data
            localeList: Translations.localeList()
            timezoneList: Translations.timezoneList()
            onLocalization: root_window.updateLocalization();
        }
                
        router: router
        
        function notifyUserAction() {
            screenSaver.notifyUserAction();
        }

        function gotoSpecialScreen(screenName) {
            Logger.debug(`gotoSpecialScreen: ${screenName}`);
            router.show(screenName);
            Logger.debug(`gotoSpecialScreen: ${screenName} COMPLETE`);
        }
        
        function goBackToStandardScreen() {
            Logger.debug(`goBackToStandardScreen: ${router.currentPath}`);
            router.show(router.currentPath);
            Logger.debug(`goBackToStandardScreen: ${router.currentPath} COMPLETE`);
        }
    }
    
    Component.onCompleted: {
        checkAvailableFonts()
    }

    function checkAvailableFonts() {
        var forceLoad = UiConst;
        var fonts = Qt.fontFamilies()
        for (var i = 0; i < fonts.length; i++) {
            Logger.debug(`${Qt.font({ "family":fonts[i]})} is available.`)
        }
    }

    function logFontDetails(fontLoader) {
        Logger.debug(`Name: ${fontLoader.name}`);
        Logger.debug(`  Props: ${Object.keys(fontLoader)}`);
        Logger.debug(`  Source: ${fontLoader.source}`);
        var statuses = {};
        statuses[FontLoader.Null] = "Null";
        statuses[FontLoader.Ready] = "Ready";
        statuses[FontLoader.Loading] = "Loading";
        statuses[FontLoader.Error] = "Error";
        Logger.debug(`  Status: ${statuses[fontLoader.status]}`);
        var font = Qt.font({ "family":fontLoader.name });
        Logger.debug(`  Font: ${font}`);
    }

}
