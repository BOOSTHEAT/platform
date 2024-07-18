import QtQml 2.13
import QtQuick 2.13
import QtQuick.Controls 2.13
import QtQuick.Layouts 1.13

import Shared 1.0

ComboBox {
    id: root

    property var runtime
    property int receivedValue

    onReceivedValueChanged: {
        currentIndex = Js.getKeyByValue(item => item.value === receivedValue, model)
    }

    delegate: ItemDelegate {
        width: root.width
        contentItem: Text {
            text: getText(root.runtime.cache, modelData)
            font.pixelSize: 16
            font.family: UiConst.fontMd
            font.weight: Font.Medium
            elide: Text.ElideRight
            horizontalAlignment: Text.AlignLeft
            leftPadding: 30
            verticalAlignment: Text.AlignVCenter
        }
        highlighted: root.highlightedIndex === index
    }

    indicator:Image{
        y: root.topPadding + (root.availableHeight - height) / 2
        x: root.width - width - root.rightPadding
        source:"../Images/nav-arrow-menu.png"
        rotation: comboPopup.visible ? 180 : 0
      }

    contentItem: Text {
        text: getText(root.runtime.cache, root.model[root.currentIndex])
        font.pixelSize: 16
        font.family: UiConst.fontMd
        font.weight: Font.Medium
        verticalAlignment: Text.AlignVCenter
        horizontalAlignment: Text.AlignHCenter
        elide: Text.ElideRight
    }

    background: Rectangle {
        implicitWidth: 120
        implicitHeight: 40
        border.width: root.visualFocus ? 2 : 1
        border.color: lightGrey
        radius: width
    }

    popup: Popup {
        id:comboPopup
        y: root.height - 1
        width: root.width
        implicitHeight: contentItem.implicitHeight
        padding: 1
        onVisibleChanged: {
            root.runtime.notifyUserAction()
        }
        contentItem: ListView {
            clip: true
            implicitHeight: contentHeight
            model: root.popup.visible ? root.delegateModel : null
            currentIndex: root.highlightedIndex

            ScrollIndicator.vertical: ScrollIndicator { }
        }

        background: Rectangle {
            border.color: lightGrey
            radius: 2
        }
    }

    function getText(cache,item) {
        return cache.hasTranslations ? cache.translate(item.key) : item.text;
    }
    
    readonly property string lightGrey: "#D6D6D6"
}
