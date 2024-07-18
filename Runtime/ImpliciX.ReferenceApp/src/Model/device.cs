using ImpliciX.Language.Model;
using ImpliciX.Language.StdLib;

namespace ImpliciX.ReferenceApp.Model;

public class device : Device
{
  public static device _ { get; } = new ();

  private device() : base(nameof(device))
  {
    other = new HardwareDeviceNode(nameof(other), this);
  }
  public HardwareDeviceNode other { get; }
}