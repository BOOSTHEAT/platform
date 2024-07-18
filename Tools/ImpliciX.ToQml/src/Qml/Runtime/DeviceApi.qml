import QtQuick 2.13
import QtQml 2.13

import Shared 1.0

QtObject {
    id: root

    signal propertiesChanged(var properties)
    signal messageToSend(string message, string key, string value)
    signal timeSeriesChanged(string jsonText)

    function handleTextReceived(message) {
        var message_json = JSON.parse(message);

        if (message_json.$type === "properties")
        {
            var properties = message_json.Properties;
            const updatedProperties = {}
            for (let p of properties) {
                const val = Object.assign(p, { at: new Date() })
                updatedProperties[p.Urn] = val;
            }
            propertiesChanged(updatedProperties);
        }
        else if (message_json.$type === "timeseries")
        {
            timeSeriesChanged(message)
        }
    }
    
    function sendProperty(key, value){
        sendKeyValue("properties", key, value);
    }
    
    function sendKeyValue(type, key, value){
        Logger.info(type, `key: ${key}, value: ${value}`);
        messageToSend(createMessage(key, value, type), key, value)
    }
    
    function createMessage(key, value, type) {
        return `{"$type": "${type}", "Properties": [{ "Urn": "${key}", "Value": "${value}", "At": "${getCurrentTimeFormatted()}" }]}`
    }

    function getCurrentTimeFormatted() {
        let time = new Date()
        return time.getHours().toString()
                + ":"
                + time.getMinutes().toString()
                + ":"
                + time.getSeconds().toString()
                + "."
                + time.getMilliseconds().toString()
    }
    
    function sendCommand(key, value = "."){
        Logger.info('command', `key: ${key}, value: ${value}`);
        messageToSend(createCommand(key, value), key, value)
    }

    function createCommand(key, value) {
        return `{"$type": "command", "Urn": "${key}", "Argument": "${value}", "At": "${getCurrentTimeFormatted()}" }`
    }
}
