using System;
using System.Collections.Generic;
using Femyou;

namespace ImpliciX.FmuDriver.Tests
{
    public class SpyFmuIO : IFmuIO
    {
        public List<double> WrittenReals = new List<double>();
        public List<bool> WrittenBools = new List<bool>();

        public IEnumerable<double> Read(IEnumerable<IVariable> fmuVariables)
        {
            throw new NotImplementedException();
        }

        public void WriteBoolean((IVariable, bool) valueTuple)
        {
            WrittenBools.Add(valueTuple.Item2);
        }

        public void WriteReal((IVariable, double) valueTuple)
        {
            WrittenReals.Add(valueTuple.Item2);
        }

        public void WriteString(params (IVariable, string)[] valueTuples)
        {
            throw new NotImplementedException();
        }
    }
}