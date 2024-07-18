import QtTest 1.2
import Runtime 1.0
import Helpers 1.0
import Shared 1.0

TestCase {
    id: root
    name: "cache"

    function test_cache_can_translate() {
        cacheCanTranslate.locale = '0';
        compare(cacheCanTranslate.hasTranslations, true);
        compare(cacheCanTranslate.translate('Y'), 'Yes');
        compare(cacheCanTranslate.translate('N'), 'No');
        compare(cacheCanTranslate.translate('M'), 'Maybe');

        cacheCanTranslate.locale = '2';
        
        compare(cacheCanTranslate.translate('Y'), 'Oui');
        compare(cacheCanTranslate.translate('N'), 'Non');
        compare(cacheCanTranslate.translate('M'), 'Peut-être');
    } 

    Cache {
        id: cacheCanTranslate
        translations: theTranslations
        localeList : ['en_GB','es_PH', 'fr_FR']
    }

    function test_cache_without_locale_has_no_translations() {
        compare(cacheWithoutLocale.hasTranslations, false);
    } 

    Cache {
        id: cacheWithoutLocale
        translations: theTranslations
        localeList : ['en_GB','es_PH', 'fr_FR']
    }

    function test_cache_without_translations_has_no_translations() {
        compare(cacheWithoutTranslations.hasTranslations, false);
    } 

    Cache {
        id: cacheWithoutTranslations
        locale: 91
        localeList : ['en_GB','es_PH', 'fr_FR']
    }

    function test_cache_with_empty_translations_has_no_translations() {
        compare(cacheWithEmptyTranslations.hasTranslations, false);
    } 

    Cache {
        id: cacheWithEmptyTranslations
        locale: 91
        translations: {}
        localeList : ['en_GB','es_PH', 'fr_FR']
    }
    
    function test_localizeDateTime() {
        var tz = '2'; // Asia__Hong_Kong
        var loc = '2'; // fr_FR
        var localizedDateTime = cacheTimeZone.localizeDateTime('2023-11-30T15:00:00',tz,loc,cacheTimeZone.timezoneList,cacheTimeZone.localeList);          
        compare(localizedDateTime.toString(), 'Thu Nov 30 2023 23:00:00 GMT+0800');
        
        var tz = '0'; // Europe__Paris
        var loc = '1'; // en_US
        var localizedDateTime = cacheTimeZone.localizeDateTime('2023-11-30T15:00:00',tz,loc,cacheTimeZone.timezoneList,cacheTimeZone.localeList);          
        compare(localizedDateTime.toString(), 'Thu Nov 30 2023 16:00:00 GMT+0100');    
    }
    
    Cache {
        id: cacheTimeZone
        timezoneList : ['Europe__Paris','Africa__Libreville', 'Asia__Hong_Kong']
        localeList : ['en_GB','en_US', 'fr_FR']        
    }   
    
    property var theTranslations : {
     'en': {
       'Y': "Yes",
       'N': "No",
       'M': "Maybe",
     },
     'fr': {
       'Y': "Oui",
       'N': "Non",
       'M': "Peut-être",
     },
    }
    

}