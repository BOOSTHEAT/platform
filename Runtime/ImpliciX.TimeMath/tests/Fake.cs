using ImpliciX.Language.Model;

namespace ImpliciX.TimeMath.Tests;

public class Fake : RootModelNode
{
  static Fake()
  {
    var root = new Fake();
  }

  private Fake() : base(nameof(Fake).ToLower())
  {
  }
}
