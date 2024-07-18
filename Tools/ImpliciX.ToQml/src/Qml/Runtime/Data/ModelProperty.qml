import QtQuick 2.13

QtObject {
    property var value : undefined
    property var at
    
    function display() {
        return value ? value : ""
    }
}
