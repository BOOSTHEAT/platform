using System;
using System.Diagnostics.Contracts;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples.Functions
{
  public class Polynomial1
  {
    public static FuncRef Func => new FuncRef(nameof(Polynomial1), () => Runner, xUrns=>xUrns);
    public static FunctionRun Runner =>
      (functionDefinition, xs) =>
      {
        Contract.Assert(xs.Length == 1, "Polynomial1 should have one variable");
        float result = 0;

        foreach (var param in functionDefinition.Params)
        {
          var paramName = param.Key;
          var hasParsedParam = int.TryParse(paramName.Substring(1), out int i);

          Contract.Assert(paramName[0] == 'a' && hasParsedParam,
            $"Invalid parameter {paramName}, should be \"ak\", k being an integer.");
          result += param.Value * MathF.Pow(xs[0].value, i);
        }

        return result;
      };
  }
}