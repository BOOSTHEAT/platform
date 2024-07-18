using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.FiniteStateMachine;

namespace ImpliciX.SharedKernel.DocTools
{
  public class CompositeStateViewModel : StateViewModel
  {
    private CompositeStateViewModel(StateViewModel svm) : base(svm)
    {
    }
    public new static CompositeStateViewModel Create<T>(StateDefinition<T, DomainEvent, DomainEvent> stateDefinition)
    {
      return new CompositeStateViewModel(StateViewModel.Create(stateDefinition));
    }
  }
}