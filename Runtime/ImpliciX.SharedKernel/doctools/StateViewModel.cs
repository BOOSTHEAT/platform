using ImpliciX.Language.Core;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SharedKernel.DocTools
{
  public class StateViewModel
  {
    protected StateViewModel(StateViewModel svm)
    {
      Name = svm.Name;
      ParentName = svm.ParentName;
      IsInitialSubState = svm.IsInitialSubState;
    }
    private StateViewModel(string name, Option<string> parentName, bool isInitialSubState)
    {
      Name = name;
      ParentName = parentName;
      IsInitialSubState = isInitialSubState;
    }

    protected static StateViewModel Create<T>(StateDefinition<T, DomainEvent, DomainEvent> stateDefinition)
    {
      var state = stateDefinition.GetPrivatePropertyValue<T>("Alias");
      var parent = stateDefinition.GetPrivatePropertyValue<Option<T>>("ParentState");
      return new StateViewModel(state.ToString(), parent.Map(x => x.ToString()), stateDefinition.IsInitialSubState);
    }
    public string Name { get; }
    public Option<string> ParentName { get; }
    public bool IsInitialSubState { get; }
  }
}