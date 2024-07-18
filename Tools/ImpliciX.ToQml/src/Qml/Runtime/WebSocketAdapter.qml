import QtQuick 2.13
import QtWebSockets 1.0

import Shared 1.0

QtObject {
    id: wsAdapter
    
    property var ws: client
    property bool isOpened: false

    signal textMessageReceived(string message)
    signal close()

    property WebSocket client: WebSocket {
        active: false
        url: "ws://" + Js.getArgument("backend","127.0.0.1:9999")

        onTextMessageReceived: wsAdapter.textMessageReceived(message)

        onErrorStringChanged: {
            if (errorString != "")
            {
                Logger.error(qsTr("Server error: %1").arg(errorString));
            }
        }

        onStatusChanged: {
            wsAdapter.isOpened = false;
            
            if (client.status === WebSocket.Open)
            {
                Logger.info(`Websocket connected to ${url}`)
                timer_retry.running = false;
                wsAdapter.isOpened = true;
            }
            else if (client.status === WebSocket.Closed) {
                Logger.warning("Websocket closed")
                active = false;
                timer_retry.running = true;
                wsAdapter.close();
            }
        }
    }

    function send(message) {
        client.sendTextMessage(message);
    }

    property Timer timer_retry: Timer {
        interval: 5000; running: true; repeat: true
        onTriggered: {
            client.active = true
        }
    }
}
