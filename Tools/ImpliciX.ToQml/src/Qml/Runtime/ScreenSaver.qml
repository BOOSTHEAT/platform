import QtQuick 2.13
import Shared 1.0

Timer {
    id: timer
    
    property int timeout : 0
    property bool isPresent : false
    property bool isIn : false
    
    interval: timeout
    running: isPresent
    repeat: false
    
    onIsPresentChanged: {
        //Logger.debug(`SCREEN SAVER isPresent changed to ${isPresent} with interval=${interval}`);
        timer.restart();
    }
        
    onTriggered: {
        //Logger.debug(`SCREEN SAVER timeout`);
        if (isPresent) {
            //Logger.debug(`SCREEN SAVER entering screen saver`);
            isIn = true;
            entered();
        }
    }
    
    function notifyUserAction() {
        //Logger.debug(`SCREEN SAVER user action detected ${(new Error).stack}`);
        if (isPresent) {
            //Logger.debug(`SCREEN SAVER restarting timer`);
            timer.restart();
            if(isIn) {
                isIn = false;
                leave();
            }
        }
    }
    
    signal entered()
    signal leave()
}
