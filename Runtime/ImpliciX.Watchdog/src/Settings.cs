using System.Collections.Generic;

namespace ImpliciX.Watchdog
{
  public class Settings
  {
    public Dictionary<string,string> Modules { get; set; }
    public int PanicDelayBeforeRestart{ get; set; }
  }
}