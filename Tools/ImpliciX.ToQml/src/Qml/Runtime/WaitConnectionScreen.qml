import QtQuick 2.13
import Shared 1.0

QtObject {

    property var runtime : undefined
    property bool isPresent : false

    readonly property bool isActive : isPresent && runtime !== undefined && !runtime.isConnected
   
    Component.onCompleted: {
        Logger.debug(`STARTUP WAIT CONNECTION isActive changed to ${isActive} with isPresent=${isPresent} and runtime.isConnected=${runtime.isConnected}`);
        if(isActive)
            entered();
    }
    
    onIsActiveChanged : {
        Logger.debug(`WAIT CONNECTION isActive changed to ${isActive} with isPresent=${isPresent} and runtime.isConnected=${runtime.isConnected}`);
        if(isActive)
            entered();
        else
            leave();
    }
    
    signal entered()
    signal leave()
}
