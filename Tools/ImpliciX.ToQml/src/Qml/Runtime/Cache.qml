import QtQuick 2.13
import QtQml 2.13

import Shared 1.0

QtObject {
    id: root

    function update(properties) {
        for (let p of Object.values(properties)) {
            var propName = p.Urn.replace(/:/g, '$');
            if (root[propName]) {
                Logger.verbose(`cache update ${propName} ${p.Value} ${p.at}`)
                root[propName].value = p.Value
                root[propName].at = p.at
            }
        }
    }
    
   function updateTimeSeries(jsonText) {
        var jsonObject = JSON.parse(jsonText)
        for(var dataPointUrn in jsonObject.DataPoints)
        {
            var tsCacheUrn = "timeSeries$" + dataPointUrn.replace(/:/g, '$');
            if (root[tsCacheUrn])
            {
                var dataPoint = jsonObject.DataPoints[dataPointUrn]
                root[tsCacheUrn].setDataPoints(dataPoint)
            }
        }
    }
    
    property var locale : undefined
    property var timeZone : undefined
    property var translations : undefined
    property var localeList : undefined
    property var timezoneList : undefined
    property var hasTranslations : !!locale && !!translations && !!localeList
    property var translate : create(translations,locale,localeList)
    
    signal localization()
    onLocaleChanged: localization()
    onTimeZoneChanged: localization()
    
    function create(dictionary, codeLang, locales) {
        let lang = locales[codeLang]
        if (!dictionary || !lang) {
            return spot => `¡ ${spot} !`;
        }
    
        function getTranslation(spot) {
            let languageCode = getPrimaryLanguageCode(lang);
            let text = dictionary[languageCode] && dictionary[languageCode][spot];
    
            return text !== undefined && text !== "" ? text : `¡ ${spot} !`;
        }
    
        function getPrimaryLanguageCode(lang) {
            if (dictionary[lang]) {
                return lang;
            }
            return lang.split("_")[0];
        }
    
        return getTranslation;
    }

    property var now : new Date()
    // we have to use JsUtil/moment.com because qtqml/toLocaleTimeString() does not manage timezone
    property var nowLocal: localizeDateTime(now,timeZone,locale,timezoneList,localeList)
    
    function localizeDateTime(dt,codeTz,codeLoc,timezones,locales) {
        let tz = timezones[codeTz]
        let loc = locales[codeLoc]
        let m = M.moment(dt);
        // console.log(`localize ${dt} -- ${m}`);
        if(!tz || !loc) {
            return m;
        }
        return m.tz(tz.replace("__","/")).locale(loc);
    }

    property Timer timer: Timer{
        interval: 500;
        running: true;
        repeat: true

        onTriggered: {
            now = new Date()
        }
    }
}






