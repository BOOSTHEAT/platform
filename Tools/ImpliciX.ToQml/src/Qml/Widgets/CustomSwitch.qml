import QtQuick 2.13
import QtQuick.Controls 2.13

import Shared 1.0

Switch {
    id: control
    checked: true
    property bool greySwitch : false
    onClicked: {
        root.runtime.notifyUserAction()
    }
    Rectangle{
        //shadow
        implicitWidth: parent.width
        implicitHeight: parent.height

        anchors{
            left: parent.left
            top: parent.top
            leftMargin: 3
            topMargin: 2
        }

        radius: 27
        color: lightGrey
        visible: !control.greySwitch
    }

    Rectangle { // rectangle arrondi contenant la bulle "On" ou "Off"
        //to avoid green color on some screens, we cannot use 'opacity'
        // so we make visible this alternate 'bulle' Rectangle
        visible: control.greySwitch
        implicitWidth: parent.width
        implicitHeight: parent.height
        radius: 27
        color: UiConst.lightGrey
        Rectangle {   // bulle contenant le mot "On" ou "Off"
            x: control.checked ? 60 : 3
            y: 3
            width: 36
            height: 35
            radius: 50
            color: lightGrey
            border.color: white2
            border.width: 1
        }
    }

    indicator:  Rectangle { // rectangle arrondi contenant la bulle "On" ou "Off"
        // avec le switch enabled (normal defaut case)
        visible: !control.greySwitch
        implicitWidth: parent.width
        implicitHeight: parent.height
        radius: 27
        color: darkGrey2
        border.width: 1
        border.color: lightGrey

        ColorAnimation on color {

            to: white
            duration: 200
            running: !control.checked
        }

        ColorAnimation on color {

            to: darkGrey2
            duration: 200
            running: control.checked
        }

        Rectangle {   // bulle contenant le mot "On" ou "Off"
            x: control.checked ? 60 : 3
            y: 3
            width: 36
            height: 35
            radius: 50
            color: darkGrey2
            border.color: lightGrey
            border.width: 1

            NumberAnimation on x {
                duration: 100
                running: control.checked
                from:3
                to: 60
            }
            NumberAnimation on x {
                duration: 100
                running: !control.checked
                from : 60
                to : 3
            }

            ColorAnimation on color {

                to: white
                duration: 200
                running: control.checked
            }
            ColorAnimation on color {

                to: darkGrey2
                duration: 200
                running: !control.checked
            }
        }
    }
    
    readonly property string white: "white"
    readonly property string white2: "#f6f3f3"
    readonly property string darkGrey2: "#505050"
    readonly property string lightGrey: "#D6D6D6"

}

