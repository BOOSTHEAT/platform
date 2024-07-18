using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ImpliciX.Language.Core;

namespace ImpliciX.Data.Factory
{
    public class HashValue
    {
        public const string SingleValueFieldName = "value";
        public const string TimeSpanFieldName = "at";

        private readonly List<(string Name, string Value)> _allValues;

        public string Key { get; }

        public (string Name, string Value)[] ValuesWithoutAtField =>
            _allValues.Where(v => !v.Name.Equals(TimeSpanFieldName, StringComparison.OrdinalIgnoreCase)).ToArray();

        public (string Name, string Value)[] Values => _allValues.ToArray();

        public HashValue(string key, string value, TimeSpan? at)
        {
            Key = key;
            var v1 = (SingeValueFieldName: SingleValueFieldName, value);
            var v2 = at.HasValue
                ? (TimeSpanFieldName, TimeSpanAsString(at.Value))
                : default((string, string)?);

            _allValues = new[] {v1, v2}
                .Where(v => v.HasValue)
                .Select(v => v.Value).ToList();
        }

        public HashValue(string key, (string Name, string Value)[] allValues)
        {
            Key = key;
            _allValues = allValues.ToList();
        }

        public Option<(string Value, string At)> GetSingleValue()
        {
            var nameValues = _allValues.Where(v => v.Name.Equals(SingleValueFieldName)).ToArray();
            var atValues = _allValues.Where(v => v.Name.Equals(TimeSpanFieldName)).ToArray();

            return nameValues.Length == 1 && atValues.Length == 1
                ? (nameValues.First().Value, atValues.First().Value)
                : Option<(string Value, string At)>.None();
        }

        private HashValue SetField(string name, string value)
        {
            var field = _allValues.SingleOrDefault(c => c.Name == name);
            if (field.Name != null)
            {
                _allValues.Remove(field);
            }

            _allValues.Add((name, value));
            return this;
        }

        public HashValue SetAtField(TimeSpan ts) => SetField(TimeSpanFieldName, TimeSpanAsString(ts));

        public Result<TimeSpan?> At(Func<string, Error> fnError)
        {
            var strTime = _allValues.FirstOrDefault(v => v.Name.Equals(TimeSpanFieldName, StringComparison.OrdinalIgnoreCase)).Value;
            if (strTime == null) return Result<TimeSpan?>.Create(default(TimeSpan?));
            if (!TimeSpan.TryParse(strTime, CultureInfo.InvariantCulture, out var timeSpan))
            {
                return fnError($"{strTime} is not a valid TimeSpan value");
            }

            return timeSpan;
        }

        public static string TimeSpanAsString(TimeSpan ts)
        {
            return ts.ToString("g", CultureInfo.InvariantCulture);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HashValue hash)) return false;
            return Key == hash.Key && ValuesWithoutAtField.SequenceEqual(hash.ValuesWithoutAtField);
        }

/*
        public override bool Equals(object obj)
        {
            if (!(obj is HashValue hash)) return false;
            return Key == hash.Key && _allValues.SequenceEqual(hash._allValues);
        }
*/
        public override int GetHashCode()
        {
            return HashCode.Combine(Key, ValuesWithoutAtField);
        }

        public override string ToString() => $"({Key},[{string.Join(',',ValuesWithoutAtField)}])";
    }
}