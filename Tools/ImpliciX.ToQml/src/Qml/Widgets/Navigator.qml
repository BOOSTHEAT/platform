import QtQuick 2.13
import QtQuick.Controls 2.13

RadioButton {
    property var visual
    property var checkedMark
    property var route
    checkable: true
    onClicked: {
        root.runtime.notifyUserAction()
        root.runtime.router.navigate(route)
    }
    checked: root.runtime.router.currentPath.startsWith(route)
    data: checked ? [visual,checkedMark] : [visual]
}
