import QtQuick 2.13
import QtQuick.Controls 2.13
import Shared 1.0

Item {
    property string defaultPath
    property var routes: []

    property var initialized: false
    property var stackView
    property var runtime
    property var store : {}
    property var sender : {}
    property var history: []
    property var currentPath: undefined
    property var previouslyDisplayedPath: undefined
    property var displayedPath: undefined
    property var showStartupPath: undefined
    property var navigateStartupPath: undefined

    Component.onCompleted: {
        Logger.debug(`router initialisation`);
        initialized = true;
        navigate(navigateStartupPath || defaultPath);
        navigateStartupPath = undefined;
        showStartupPath = undefined;
        Logger.debug(`router initialisation complete`);
    }

    function navigate(askPath, nextArgs) {
        if (!initialized) {
            navigateStartupPath = path;
            return;
        }

        if (!pathExists(askPath)) {
            Logger.verbose(`Can't find route ${askPath}, redirect to default path: ${defaultPath}`);
        }

        const nextPath = getPathOrDefault(askPath);

        if (currentPath != nextPath) {
            Logger.verbose(`navigate currentPath:${currentPath} nextPath:${nextPath}`);
            currentPath = nextPath
            show(showStartupPath || nextPath, nextArgs)
        }
    }
    
    function show(path,args) {
        if (!initialized) {
            showStartupPath = path;
            return;
        }
        Logger.verbose(`router.show ${path}`);
        stackView.replace(
            routes[path].file,
            Js.shallowMerge(args, routes[path].args, {
                "runtime": runtime,
                "store": store,
                "sender": sender
            }),
            StackView.Immediate
        );
        previouslyDisplayedPath = displayedPath;
        displayedPath = path;
        Logger.verbose(`router.show ${path} COMPLETE`);
    }
    
    function changeCurrentPathBy(condition) {
        var newPath = getPathBy(condition);
        if(newPath !== currentPath)
          currentPath = newPath;
    }

    function getPathBy(condition) {
        return Js.getKeyByValue(
            condition,
            routes
        );
    }

    function getCurrentScreenComponent() {
        return stackView && stackView.currentItem;
    }

    function pathExists(path) {
        return !!routes[path];
    }

    function getPathOrDefault(path) {
        return pathExists(path) ? path : defaultPath;
    }

    function goBack() {
        const index = history.length -2
        navigate(history[index])
    }

    function goParent() {
        const parts = currentPath.split('/');
        parts.splice(-1,1);
        navigate(parts.join('/'))
    }

    onCurrentPathChanged: {
        Logger.info('Route', currentPath);
        history.push(currentPath)
    }
}
