using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples.Definition
{
    [ValueObject]
    public enum FunChoice { A, B }
    
    public class fun_example : RootModelNode
    {
        static fun_example()
        {
            settings = new settings(new fun_example());
            selector = new selector(new fun_example());
        }
        public fun_example() : base(nameof(fun_example))
        {
        }
        
        public static settings settings { get; } 
        public static selector selector { get; } 
    }

    public class selector : SubSystemNode
    {
        public selector(ModelNode parent) : base(nameof(selector), parent)
        {
            selected_fun =  PropertyUrn<FunctionDefinition>.Build(Urn, nameof(selected_fun));
            x =  PropertyUrn<Percentage>.Build(Urn, nameof(x));
            y =  PropertyUrn<Percentage>.Build(Urn, nameof(y));
            z =  PropertyUrn<Percentage>.Build(Urn, nameof(z));
        }
        
        public PropertyUrn<Percentage> x { get; }
        
        public PropertyUrn<Percentage> y { get; }
        
        public PropertyUrn<Percentage> z { get; }
        public PropertyUrn<FunctionDefinition> selected_fun { get; }
    }

    public class settings : ModelNode
    {
        public settings(ModelNode parent) : base(nameof(settings), parent)
        {
            fun_choice = PropertyUrn<FunChoice>.Build(Urn, nameof(fun_choice));
            fa = PropertyUrn<FunctionDefinition>.Build(Urn, nameof(fa));
            fb = PropertyUrn<FunctionDefinition>.Build(Urn, nameof(fb));
        }
        
        public PropertyUrn<FunChoice> fun_choice { get; }
        public PropertyUrn<FunctionDefinition> fa { get; }
        public PropertyUrn<FunctionDefinition> fb { get; }

        
    }
}