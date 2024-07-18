import Runtime 1.0

StandaloneRuntime {

    property DeviceApi api: DeviceApi {
        onMessageToSend: send(message)
    }

    function send(message)
    {
        var message_json = JSON.parse(message);
        if(typeof message_json.Properties !== "undefined")
          cache.update(message_json.Properties)
    }
}