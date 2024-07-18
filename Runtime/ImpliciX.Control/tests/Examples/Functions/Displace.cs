using System.Diagnostics.Contracts;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples.Functions
{
    public class Displace
    {
        public static FuncRef Func => new FuncRef(nameof(Displace), () => Runner, xUrns=>xUrns);
        public static FunctionRun Runner
        {
            get
            {
                var displace = new Displace();
                return (functionDefinition, xs) =>
                {
                    Contract.Assert(xs.Length == 1, "Displace should have one variable");
                    return displace.Execute(functionDefinition, xs[0].value);
                };
            }
        }
        private uint Count { get; set; }
        private float InitialValue { get; set; }
        private bool IsInitialized { get; set; }

        public float Execute(FunctionDefinition functionDefinition, float initialValue)
        {
            if (!IsInitialized)
            {
                InitialValue = initialValue;
                IsInitialized = true;
            }
            
            float Kd = functionDefinition.GetValueParam(nameof(Kd));

            var result = InitialValue + Count++ * Kd;
            return result;
        }
    }
}