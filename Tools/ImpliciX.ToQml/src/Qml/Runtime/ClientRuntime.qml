import QtQuick 2.13
import Shared 1.0

StandaloneRuntime {

    isConnected: false
    
    property WebSocketAdapter wsAdapter : WebSocketAdapter {
        onTextMessageReceived: api.handleTextReceived(message)
        onIsOpenedChanged: isConnected = wsAdapter.isOpened
    }

    property DeviceApi api: DeviceApi {
        onPropertiesChanged: cache.update(properties)
        onMessageToSend: wsAdapter.send(message)
        onTimeSeriesChanged: cache.updateTimeSeries(jsonText)
    }
}
