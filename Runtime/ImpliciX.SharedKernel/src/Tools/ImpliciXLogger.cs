using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Logger;

namespace ImpliciX.SharedKernel.Tools
{
    public static class ImpliciXLogger
    {
        public static ILog Create(Dictionary<string, string> tagSet)
        {
            return DelegateLogger.Create(new SerilogLogger(Serilog.Log.Logger.ForContext("Tags", tagSet)));
        }
    }
}