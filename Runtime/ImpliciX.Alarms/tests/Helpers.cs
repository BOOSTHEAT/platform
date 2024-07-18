using System.Linq;
using ImpliciX.Language.Model;

namespace ImpliciX.Alarms.Tests
{
  public class Helpers
  {
    public static AlarmSettings CreateSettings(int consecutiveErrorsBeforeFailure, params (Urn Urn, int)[] overrides) =>
      new AlarmSettings
      {
        ConsecutiveSlaveCommunicationErrorsBeforeFailure = new AlarmSettings.ConsecutiveErrors
        {
          Default = consecutiveErrorsBeforeFailure,
          Override = overrides.Length == 0
            ? null
            : overrides.Select(o => new AlarmSettings.ConsecutiveErrors.SlaveErrors
              { Slave = o.Urn.ToString(), Value = o.Item2 }).ToArray()
        }
      };
  }
}