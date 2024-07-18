using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SharedKernel.DocTools
{
  public class LeafStateViewModel : StateViewModel
  {
    private LeafStateViewModel(StateViewModel svm) : base(svm)
    {
    }

    public new static LeafStateViewModel Create<T>(StateDefinition<T, DomainEvent, DomainEvent> stateDefinition)
    {
      return new LeafStateViewModel(StateViewModel.Create(stateDefinition));
    }
  }
}