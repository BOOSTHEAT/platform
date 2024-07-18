using ImpliciX.Language.Model;

namespace ImpliciX.Data.Tests.HashDb;

public class root : RootModelNode
{
  public root() : base(nameof(root))
  {
  }

  static root()
  {
    var self = new root();
    temperature = PropertyUrn<Temperature>.Build(self.Urn, nameof(temperature));
    presence = PropertyUrn<Presence>.Build(self.Urn, nameof(presence));
    function = PropertyUrn<FunctionDefinition>.Build(self.Urn, nameof(function));
  }
  
  public static PropertyUrn<Temperature> temperature { get; }
  public static PropertyUrn<Presence> presence { get; }
  public static PropertyUrn<FunctionDefinition> function { get; }

}