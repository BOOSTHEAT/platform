using System.Collections.Generic;
using Femyou;

namespace ImpliciX.FmuDriver
{
    public interface IFmuIO
    {
        IEnumerable<double> Read(IEnumerable<IVariable> fmuVariables);
        void WriteBoolean((IVariable, bool) valueTuple);
        void WriteReal((IVariable, double) valueTuple);
        void WriteString(params (IVariable, string)[] valueTuples);
    }
}