using System.Diagnostics.Contracts;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples.Functions
{
  public class Substract
  {
    public static FuncRef Func => new FuncRef(nameof(Substract), () => Runner, xUrns=>xUrns);

    public static FunctionRun Runner =>
      (functionDefinition, xs) =>
      {
        Contract.Assert(xs.Length == 2, "Substract should have two variables");
        var result = xs[0].value - xs[1].value;
        return result;
      };
  }
}