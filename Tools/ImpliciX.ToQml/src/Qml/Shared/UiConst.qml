
pragma Singleton
import QtQuick 2.5
import QtQml 2.5

QtObject {

    // The names of the font are mandatory for this
    // to work on Linux. This name shall match the
    // font family name defined in the font source
    // file
    readonly property var swissFontLt: FontLoader {
        name: "Swiss721 Lt BT"
        source: "../Fonts/Swiss721BT-Light.ttf"
    }
    readonly property var swissFontBTR: FontLoader {
        name: "Swiss721 BT"
        source: "../Fonts/Swiss721BT-Regular.ttf"
    }
    readonly property var swissFontMd: FontLoader {
        name: "Swiss721 Md BT"
        source: "../Fonts/Swiss721BT-Medium.ttf"
    }
    readonly property var swissFontHv: FontLoader {
        name: "Swiss721 Hv BT"
        source: "../Fonts/Swiss721BT-Heavy.ttf"
    }
    readonly property var proximaNovaEb: FontLoader {
        name: "Proxima Nova"
        source: "../Fonts/ProximaNovaExtraBold.otf"
    }
    readonly property string fontLt : swissFontLt.name
    readonly property string fontMd : swissFontMd.name
    readonly property string fontHv : swissFontHv.name
    readonly property string fontBtr : swissFontBTR.name
    readonly property string fontPnEb : proximaNovaEb.name
}
