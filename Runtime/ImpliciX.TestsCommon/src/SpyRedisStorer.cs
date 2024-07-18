using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.TestsCommon
{
    public class RedisStorerSpy
    {
        public readonly List<(string key,string value,TimeSpan when)> RecordedKeyValues = new List<(string,string,TimeSpan)>();
        public bool Store(IEnumerable<IDataModelValue> modelValue)
        {
            modelValue.ToList().ForEach(mv=>RecordedKeyValues.Add((mv.Urn,mv.ToString(),mv.At)));
            return true;
        }
    }
}