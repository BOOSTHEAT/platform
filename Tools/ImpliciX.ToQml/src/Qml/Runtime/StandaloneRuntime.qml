import QtQuick 2.13
import Shared 1.0

QtObject {

    property Cache cache
    property Router router
    property DeviceApi api: DeviceApi {
    }
    property bool isConnected : true
    
}
