using ImpliciX.Language.Model;
using ImpliciX.ReferenceApp.Model.Tree;

namespace ImpliciX.ReferenceApp.Model;

public class system : RootModelNode
{
  static system()
  {
    var rootNode = new system();

    metrics = new metrics(nameof(metrics), rootNode);
    timemath = new metrics(nameof(timemath), rootNode);
    processing = new processing(nameof(processing), rootNode);
  }


  private system() : base(nameof(system))
  {
  }

  public static metrics metrics { get; }
  public static metrics timemath { get; }
  public static processing processing { get; }
}