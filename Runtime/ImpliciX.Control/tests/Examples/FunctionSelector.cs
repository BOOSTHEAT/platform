using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Control.Tests.Examples.Functions;
using ImpliciX.Language.Control;

namespace ImpliciX.Control.Tests.Examples
{
    
    public class FunctionSelector : SubSystemDefinition<FunctionSelector.State>
    {
        public enum State
        {
            Run,                
        }

        public FunctionSelector()
        {
            // @formatter:off
            Subsystem(fun_example.selector)
                .Always
                    .Set(fun_example.selector.selected_fun)
                        .With(fun_example.settings.fb).When(Condition.Is(fun_example.settings.fun_choice,FunChoice.B))
                        .With(fun_example.settings.fa).Otherwise()
                .Initial(State.Run)
                .Define(State.Run)
                     .OnState
                        .Set(fun_example.selector.y,Polynomial1.Func, fun_example.selector.selected_fun, fun_example.selector.x)
                        .Set(fun_example.selector.z, fun_example.selector.x)
                ;
            // @formatter:on
        }
    }
    
    
    
}