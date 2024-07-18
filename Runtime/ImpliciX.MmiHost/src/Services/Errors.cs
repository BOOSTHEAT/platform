using System;
using ImpliciX.Language.Core;

namespace ImpliciX.MmiHost.Services
{
    public static class Errors
    {
        public static Error BspVersionNotFound(string variableName)
        {
            return new Error(nameof(BspVersionNotFound), $"No value for environment variable {variableName}");
        }

        public static Error UpdateBspError(Exception ex)
        {
            return new Error(nameof(UpdateBspError), ex.CascadeMessage());
        }
    }
}