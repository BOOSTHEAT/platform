.pragma library
.import "JsUtils.js" as Js

function unitResult(value,unit) {
    return {
        value:value,
        unit:unit
    }
}
function none(value) {
    return unitResult(value,"")
}
function toCelsius(value) {
    const ret = (value - 273.15).toFixed(0) // if value = 274.1 returns "-0"
    return unitResult(ret === "-0" ? "0" : ret,"°C")
}
function toRpm(value) {
    return Math.floor(Js.toFloat(value))
}
function toKw(val) {
    const rounded = Math.round(val * 10) / 10000   // 6750 reçu => affichera 6.8 kw
    return unitResult(rounded.toFixed(1), " kW")  // to always have one decimal 6000 => 6.0 kw
}
function toBar(value) {
    return unitResult(Number.parseFloat(value/100000).toFixed(1)," bar")
}
function toPercentage(value) {
    return unitResult((value * 100).toFixed(0),"%")
}
function toFlow(value) {  // received cubic meter per second
    const lm = value * 60000 // converted into meter per minute
    return Math.round(lm* 10) / 10 // rounding one digit
}
function toDBm(value) {  // received millwatts transformed into dBm
    if (value === "-") return "-"
    const dBm = 10*Math.log10(value).toFixed(0)
    return dBm
}

function toKwh(value) {
    const kwh = value / (3600 * 1000);
    return unitResult(kwh.toFixed(1), " kWh");
}

function toTimezone(value) {
    // "Europe__London" becomes "Europe/London"
    return (value || "").replace("__","/")
}
function toLocale(value) {
    // "en__FR" becomes "en-FR"
    return (value || "").replace("__","-")
}

// This function is dedicated to motors mean speed
// If the corresponding value is less to 30 it should
// be zero (epsilon approximation).
function minBind(value) {
    if (value < 30) {
        return 0
    }

    return value
}
