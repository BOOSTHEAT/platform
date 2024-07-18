import QtQuick 2.13
import Shared 1.0

QtObject {
    property var measure
    property var status
    
    function setValue(x) {
      measure.value = x[0];
      status.value = x[1];
    }
    
    function getValue(conversion) {
        if( status.value === undefined
            || status.value === false
            || status.value === "Failure"
            || measure.value === undefined )
           return undefined
        var result = conversion(measure.value)
        return result.value
    }
    
    function getFormattedValue(conversion,displayUnit) {
        if( status.value === undefined
            || status.value === false
            || status.value === "Failure"
            || measure.value === undefined )
           return "-"
        var result = conversion(measure.value)
        return result.value + (displayUnit ? result.unit : "")
    }
}
