namespace ImpliciX.Designer.ViewModels
{
  public class CompositeStateViewModel : BaseStateViewModel
  {
    public CompositeStateViewModel(string name, int index, CompositeDefinitionViewModel definition, params BaseStateViewModel[] children) : base(name,index,definition,children)
    {
    }
  }
}