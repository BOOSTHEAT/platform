.pragma library

const compose = (...fns) =>
    fns.reduce((f, g) => (...args) => f(g(...args)));

const pipe = (...fns) => compose(...fns.reverse());

function identity(value) {
    return value
}

function toFloat(value) {
    return parseFloat(value)
}

function mathMinOfNotNullValues(...values) {
    let validValues = []
    for (let i = 0; i < values.length; i++) {
        if(values[i] !== 0)
            validValues.push(values[i]);
    }
    if(validValues.length > 0)
        return Math.min(...validValues);
    return 0;
}
function shallowMerge() {
    const merged = {};

    function _merge(obj) {
        for (let prop in obj) {
            if (obj.hasOwnProperty(prop)) {
                merged[prop] = obj[prop];
            }
        }
    }

    for (let i = 0; i < arguments.length; i++) {
        _merge(arguments[i]);
    }

    return merged;
}

function getKeyByValue(fnCondition, obj) {
    return Object
        .entries(obj)
        .find(([,v]) => fnCondition(v))[0];
}

const eq = v => e => e === v;

function all() {
    const predicates = arguments;
    return obj => {
        for (let i = 0; i < predicates.length; i++) {
            if (!predicates[i](obj)) return false;
        }
        return true;
    }
}

function any() {
    const predicates = arguments;
    return obj => {
        for (let i = 0; i < predicates.length; i++) {
            if (predicates[i](obj)) return true;
        }
        return false;
    }
}

function prop(name) {
    return obj => {
        return (obj || {})[name];
    }
}

function uniqBy(selector) {
    return arr => {
        const flags = {}, output = [], l = arr.length;
        for (let i=0; i < arr.length; i++) {
            const key = selector(arr[i]);
            if (flags[key]) continue;
            flags[key] = true;
            output.push(arr[i]);
        }
        return output;
    }
}

function tap(fn) {
    return o => {
        fn(o);
        return o;
    }
}

function isNil(o) {
    return o === undefined || o === null;
}

const _defaultTo = defaultValue => o => isNil(o) ? defaultValue : o;

function defaultTo(defaultValue, o) {
    if (o === undefined) {
        return _defaultTo(defaultValue);
    }
    return defaultTo(defaultValue)(o);
}

function Descending(selector){
    return (a,b) => (selector(b) > selector(a)) ? 1 : ((selector(a) > selector(b) ? -1 : 0));
}

function isEmpty(arr) {
    return !arr.length;
}

function ifElse(cond, thenFn, elseFn) {
    return o => {
        if (cond(o)) {
            return thenFn(o);
        } else {
            return elseFn(o);
        }
    }
}

function when(cond, thenFn) {
    return o => {
        if (cond(o)) return thenFn(o);
    }
}

function always(o) {
    return () => o;
}

function getArgument(name, defaultValue) {
    let selectedValue = defaultValue;
    const head = name+"=";
    for (let i = 0; i < Qt.application.arguments.length; i++) {
        const arg = Qt.application.arguments[i].replace(/^\-*/, "");
        if (arg.startsWith(head)) {
            selectedValue = arg.substring(head.length);
            break;
        }
    }
    return selectedValue;
}

function mapToModelValues(values) {
    return Object.entries(values)
        .map(([urn, { Value }]) => ({ [urn]: { Urn: urn, Value}}))
        .reduce((acc, v) => Object.assign(acc, v), {})
}

function deepObjectLog(prefix, obj, shift) {
    shift = shift || 0;
    var entries = Object.entries(obj);
    for (const [key, value] of entries) {
        if(obj == null) {
            console.log(`${prefix} ${'  '.repeat(shift)}${key}: ${value}`);
        }
        else if(typeof value == "object") {
            console.log(`${prefix} ${'  '.repeat(shift)}${key}:`);
            deepObjectLog(prefix,value,shift+2);
        }
        else {
            console.log(`${prefix} ${'  '.repeat(shift)}${key}: ${value}`);
        }
    }
}