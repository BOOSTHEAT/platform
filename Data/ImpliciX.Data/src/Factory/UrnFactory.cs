using System;
using ImpliciX.Language.Core;

namespace ImpliciX.Data.Factory
{
    public static class UrnFactory
    {
        internal static Result<object> Create(Type urnType, string urn)
        {
            var factoryArgs = new object[] {new string[] {urn}};
            return Reflector.CreateInstance(urnType, factoryArgs);
        }
    }
}