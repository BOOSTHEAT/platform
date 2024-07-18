import QtQuick 2.13
import Shared 1.0

Text { id: root
    property string myColor
    height: 30
    width: 30

    font.family: UiConst.fontLt
    font.hintingPreference: Font.PreferNoHinting
    font.letterSpacing: 0
    font.pixelSize: 22
    color: myColor ? myColor : UiConst.darkGrey
    horizontalAlignment: Text.AlignHCenter
    verticalAlignment: Text.AlignVCenter
    wrapMode: Text.WordWrap
}
