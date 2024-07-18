import QtQuick 2.13
import QtQuick.VirtualKeyboard 2.13
import QtQuick.Layouts 1.13

KeyboardLayout {
    inputMode: InputEngine.InputMode.Latin
    keyWeight: 160
    readonly property real normalKeyWidth: normalKey.width
    readonly property real functionKeyWidth: mapFromItem(normalKey, normalKey.width / 2, 0).x
    KeyboardRow {
        Key {
            key: Qt.Key_B
            text: "b"
            alternativeKeys: "|_"
            smallText: "|_"
            smallTextVisible: true
        }
        Key {
            id: normalKey
            key: Qt.Key_E
            text: "é"
        }
        Key {
            key: Qt.Key_P
            text: "p"
            alternativeKeys: "&"
            smallText: "&"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_O
            text: "o"
            alternativeKeys: "ôoœ"
            smallText: "œ"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_E
            text: "è"
        }
        Key {
            key: Qt.Key_Exclam
            text: "!"
        }        
        Key {
            key: Qt.Key_V
            text: "v"
        }
        Key {
            key: Qt.Key_D
            text: "d"
        }
        Key {
            key: Qt.Key_L
            text: "l"
            alternativeKeys: "£"
            smallText: "£"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_J
            text: "j"
        }
        Key {
            key: Qt.Key_Z
            text: "z"
        }
        Key {
            key: Qt.Key_W
            text: "w"
        } 
    }
    KeyboardRow {
        Key {
            key: Qt.Key_A
            text: "a"
            alternativeKeys: "aàâæ"
            smallText: "æ"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_U
            text: "u"
            alternativeKeys: "ùuûü"
            smallText: "ù"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_I
            text: "i"
            alternativeKeys: "îiï"
        }        
        Key {
            key: Qt.Key_E
            text: "e"
            alternativeKeys: "€"
            smallText: "€"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_Comma
            text: ","
            alternativeKeys: ";"
            smallText: ";"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_C
            text: "c"
            alternativeKeys: "©"
            smallText: "©"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_T
            text: "t"
            alternativeKeys: "™"
            smallText: "™"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_S
            text: "s"
            alternativeKeys: "ß"
            smallText: "ß"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_R
            text: "r"
        }
        Key {
            key: Qt.Key_N
            text: "n"
        }
        Key {
            key: Qt.Key_M
            text: "m"
        }        
        Key {
            key: Qt.Key_C
            text: "ç"
            alternativeKeys: "cç"
        }
    }
    KeyboardRow {
        Key {
            key: Qt.Key_E
            text: "ê"
            alternativeKeys: "/ë"
            smallText: "/"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_A
            text: "à"
            alternativeKeys: "\â"
            smallText: "\\"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_Y
            text: "y"
            alternativeKeys: "{"
            smallText: "{"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_X
            text: "x"
            alternativeKeys: "}"
            smallText: "}"
            smallTextVisible: true
        }        
        Key {
            key: Qt.Key_Period
            text: "."
            alternativeKeys: ":"
            smallText: ":"
            smallTextVisible: true
        }
        Key {
            key: Qt.Key_K
            text: "k"
        }
        Key {
            key: Qt.Key_Question
            text: "?"
        }
        Key {
            key: Qt.Key_Q
            text: "q"
        }
        Key {
            key: Qt.Key_G
            text: "g"
        }
        Key {
            key: Qt.Key_H
            text: "h"
        }
        Key {
            key: Qt.Key_F
            text: "f"
        }
        BackspaceKey {
            weight: functionKeyWidth
            Layout.fillWidth: false
        }
    }
    KeyboardRow {
        ShiftKey {
            weight: functionKeyWidth
            Layout.fillWidth: false
        }
        SymbolModeKey {
            weight: functionKeyWidth
            Layout.fillWidth: false
        }
        SpaceKey {
        }
        HideKeyboardKey {
            weight: normalKeyWidth
            Layout.fillWidth: false
        }
        EnterKey {
            weight: functionKeyWidth
            Layout.fillWidth: false
        }
    }
}