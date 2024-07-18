import QtQuick 2.13
import QtQuick.Controls 2.13

Button {
    property var visual
    property var checkedMark
    background: Rectangle {
        color: 'transparent'
    }
    data: visual
    onClicked: {
        root.runtime.notifyUserAction()
    }
}