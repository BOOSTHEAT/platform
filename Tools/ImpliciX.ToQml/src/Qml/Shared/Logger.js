.pragma library
.import "JsUtils.js" as Js

const DEFAULT_LOG_LEVEL = "DEBUG";

let logLevel;

const logLevels = {
    VERBOSE: 1,
    DEBUG: 2,
    INFO: 3,
    WARNING: 4,
    ERROR: 5
}

function initialize(level) {
    if (!!logLevel) return;
    
    level = level || Js.getArgument("loglevel", DEFAULT_LOG_LEVEL);
    logLevel = logLevels[level];
    info(`loglevel set to ${Js.getKeyByValue(Js.eq(logLevel), logLevels)}`);
}

function verbose(message) {
    initialize();
    if (logLevel > logLevels.VERBOSE) return;
    _log("VERBOSE", message);
}

function debug(message) {
    initialize();
    if (logLevel > logLevels.DEBUG) return;
    _log("DEBUG", message);
}

function info(topic, message) {
    initialize();
    if (logLevel > logLevels.INFO) return;

    if (message === undefined) {
        _log("INFO", topic);
    } else {
        _log("INFO", `(${topic}) ${message}`);
    }
}

function warning(message) {
    initialize();
    if (logLevel > logLevels.WARNING) return;
    _log("WARNING", message);
}

function error(message) {
    initialize();
    _log("ERROR", message);
}

function _log(topic, message) {
    console.log(`[${topic}] ${message}`);
}