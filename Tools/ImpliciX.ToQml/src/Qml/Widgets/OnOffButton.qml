import QtQuick 2.13
import Shared 1.0

CustomSwitch{
    width: 99
    height: 41
    property string title : "PowerSwitch"
    property bool displayAB: false

    MainTextFormat{
        anchors{
            horizontalCenter: parent.horizontalCenter
            bottom: parent.top
        }
        z: 1
        width: 150
        height: 30
        text: parent.title
        font.family: UiConst.fontMd
        font.pixelSize: 17
        font.weight: Font.Medium
        wrapMode: Text.NoWrap
        horizontalAlignment: Text.AlignHCenter
    }

    MainTextFormat{

        anchors{
            bottom: parent.bottom
            bottomMargin: 7
            right: parent.right
        }
        z: 1
        font.family: UiConst.fontLt
        font.pixelSize: 16
        anchors.rightMargin: 6
        font.weight: Font.Light
        visible: checked
        text: "on"
    }

    MainTextFormat{

        anchors{
            bottom: parent.bottom
            bottomMargin: 7
            left: parent.left
            leftMargin: 4
        }
        z: 1
        width: 32
        color: white2
        font.family: UiConst.fontLt
        font.pixelSize: 16
        font.weight: Font.Light
        visible: !checked
        text: "off"
    }
    
    readonly property string white2: "#f6f3f3"
}



