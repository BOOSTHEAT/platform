using System;
using ImpliciX.Language.Core;

namespace ImpliciX.Data
{
  public static class Debug
  {
    // The pre condition predicate *MUST NOT* do any side effect. If this were to happen
    // the side effect would be striped from the RELEASE versions of the application and
    // logic depending on this side effect would be broken.
    public static void PreCondition(Func<bool> predicate, Func<string> message)
    {
#if DEBUG
      VerifyCondition(predicate(), message());          
#endif
    }

    private static void VerifyCondition(bool condition, string message)
    {
      if (!condition)
      {
        throw new ContractException($"Contract is not satisfied. {message}");
      }
    }
  }
}