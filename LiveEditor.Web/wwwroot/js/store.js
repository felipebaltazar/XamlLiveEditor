let connected = false;
let connection;
let connectionId;

function startSignalR(editor, deviceId) {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("https://liveeditorapi.azurewebsites.net/LiveEditor?connectionId=" + deviceId + "&iseditor=True")
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.start().then(function () {
        console.assert(connection.state === signalR.HubConnectionState.Connected);
        connected = true;
        console.log("LiveEditor hub Connected.");
        connectionId = deviceId;

        subscribeLiveEditorEvents(editor);

    }).catch(function (err) {
        console.assert(connection.state === signalR.HubConnectionState.Disconnected);
        console.log(err);
        setTimeout(() => startSignalR(editor, deviceId), 5000)
    });
};

function subscribeLiveEditorEvents(editor) {
    connection.on('CurrentPageChanged', (newCode) => {
        editor.getModel().setValue(newCode);
    });
}

function onCodeChanged(code) {
    if (connected === true) {
        connection.invoke("CodeChanged", connectionId, code ).catch(function (err) {
            return console.error(err.toString());
        });
    }
}

function isNullOrEmpty(variable) {
    return variable === undefined ||
        variable === null ||
        variable === "null" ||
        variable === "" ||
        variable === " " ||
        variable === "\n";
}

function generateUUID() { // Public Domain/MIT
    var d = new Date().getTime();//Timestamp
    var d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now() * 1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16;//random number between 0 and 16
        if (d > 0) {//Use timestamp until depleted
            r = (d + r) % 16 | 0;
            d = Math.floor(d / 16);
        } else {//Use microseconds since page-load if supported
            r = (d2 + r) % 16 | 0;
            d2 = Math.floor(d2 / 16);
        }
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}

function randomIntFromInterval(min, max) { // min and max included 
    return Math.floor(Math.random() * (max - min + 1) + min)
}
