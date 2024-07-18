using System;
using System.IO;
using Femyou;
using ImpliciX.Language.Model;

namespace ImpliciX.FmuDriver
{
    public static class FmuWriter
    {
        public static void FromPower(PowerSupply powerSupply, IFmuIO instance, IVariable variable) =>
            instance.WriteBoolean((variable, powerSupply == PowerSupply.On));

        public static void From3WaysValvePosition(ThreeWayValvePosition threeWayValvePosition, IFmuIO instance,
            IVariable variable)
        {
            var result = threeWayValvePosition == ThreeWayValvePosition.A ? 0f : 1f;
            instance.WriteReal((variable, result));
        }

        public static void FromIFloat(IFloat value, IFmuIO instance, IVariable variable)
        {
            var result = Convert.ToDouble(value.ToFloat());
            instance.WriteReal((variable, result));
        }

        public static void FromFilePath(string relativePath, IFmuIO instance, IVariable variable)
        {
            var result = Path.GetFullPath(relativePath);
            instance.WriteString((variable,result));
        }
    }
}