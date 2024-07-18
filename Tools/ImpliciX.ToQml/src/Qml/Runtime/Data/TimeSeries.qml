import QtQuick 2.13
import Shared 1.0

QtObject {
    property var values : []
    property var timeline : []
    property real max
    property real min
    
    function setDataPoints(data) {
        var new_values = []
        var new_timeline = []
        for (var i = 0; i < data.length; i++) {
            new_values.push(data[i].Value)
            new_timeline.push(M.moment.utc(data[i].At))
        }
        timeline = new_timeline
        values = new_values
        max = Math.max(...values)
        min = Math.min(...values)
    }
    
    function setDataPointsFromString(json) {
        var data = JSON.parse(json)
        setDataPoints(data)
    }

    function getFormattedTimeline(format){
        var formated_timeline=[]
        for(var i=0; i<timeline.length; i++){
            formated_timeline.push(timeline[i].format(format))
        }
        return formated_timeline
    }

    function determineTimelineFormat() {

        if(timeline.length <= 1) {
            return "yyyy-MM-dd";
        }

        var sortedTimeline = timeline.sort((a, b) => a.unix() - b.unix())
        var first = sortedTimeline[0];
        var last = sortedTimeline[sortedTimeline.length - 1];

        var difference = last.diff(first, 'seconds');

        if(difference <= 3600) {
            return "mm:ss";
        } else if(difference <= 86400) {
            return "HH:mm";
        } else if(difference <= 604800) {
            return "MM-dd hh'h'";
        } else {
            return "yyyy-MM-dd";
        }
    }

    function getTimelineMinDate(delta) {
        if (timeline.length === 0) {
            return undefined;
        }
        if (delta === undefined) {
            delta  = 0;
        }
        var sortedTimeline = timeline.sort((a, b) => a.unix() - b.unix())
        var result =  new Date(sortedTimeline[0].valueOf()+delta);
        return result;
    }

    function getTimelineMaxDate(delta) {
        if (timeline.length === 0) {
            return undefined;
        }
        if (delta === undefined) {
            delta  = 0;
        }
        var sortedTimeline = timeline.sort((a, b) => a.unix() - b.unix())
        var result = new Date(sortedTimeline[sortedTimeline.length - 1].valueOf()+delta);
        return result;
    }

    function getHalfPeriod() {
        if (timeline.length < 1) {
            return 0;
        }
        var sortedTimeline = timeline.sort((a, b) => a.unix() - b.unix());
        var span = sortedTimeline[sortedTimeline.length - 1].valueOf()-sortedTimeline[0].valueOf();
        var halfPeriod = span/(timeline.length-1)/2;
        return halfPeriod;
    }
}
