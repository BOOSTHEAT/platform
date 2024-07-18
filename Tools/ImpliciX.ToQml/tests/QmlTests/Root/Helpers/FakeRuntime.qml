import QtQuick 2.13
import Runtime 1.0

StandaloneRuntime {
    id: root
    property var defaultPath
    property var routes
    cache: Cache {
        property var locale
        property var timeZone
    }
    router: Router {
        stackView: theStackView
        runtime: root
        defaultPath: root.defaultPath
        routes: root.routes
    }
    signal viewReplaced()
    property var theStackView: QtObject {
        property var item
        property var properties
        property var operation
        function replace(i, p, o) {
            item = i;
            properties = p;
            operation = o;
            root.viewReplaced();
        }
    }
    function notifyUserAction() {}
}
