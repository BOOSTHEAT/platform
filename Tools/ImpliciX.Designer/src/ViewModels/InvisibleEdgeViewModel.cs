namespace ImpliciX.Designer.ViewModels
{
  public class InvisibleEdgeViewModel : AvaloniaGraphControl.Edge
  {
    public InvisibleEdgeViewModel(object tail, object head)
    : base(tail, head, new DefinitionViewModel(), Symbol.None, Symbol.None)
    {

    }
  }
}