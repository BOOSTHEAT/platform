using Femyou;
using ImpliciX.Language.Core;

namespace ImpliciX.FmuDriver
{
    public class FmuLogger : ICallbacks
    {
        public void Logger(IInstance instance, Status status, string category, string message)
        {
            if (status > Status.Discard)
            {
                Log.Error(message);
            }

            if (status == Status.Warning)
            {
                Log.Warning(message);
            }
        }
    }
}