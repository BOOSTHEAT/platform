using ImpliciX.Control.Tests.Examples.ValueObjects;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples.Definition
{
    public class devices: RootModelNode
    {
        public devices() : base(nameof(devices))
        {
        }
        
        public static computer computer => new computer(new devices());
    }
    
    public class computer : SubSystemNode
    {
        public computer(ModelNode parent) : base(nameof(computer), parent)
        {
        }
        
        public fan fan => new fan(this);
        public fan2 fan2 => new fan2(this);
        public led led => new led(this);
        
        public CommandUrn<NoArg> _start => CommandUrn<NoArg>.Build(Urn, "Start");
        public CommandUrn<NoArg> _mount => CommandUrn<NoArg>.Build(Urn, "Mount");
        public CommandUrn<NoArg> _mounted => CommandUrn<NoArg>.Build(Urn, "Mounted");
        public CommandUrn<NoArg> _powerOff => CommandUrn<NoArg>.Build(Urn, "PowerOff");

        public CommandUrn<Switch> _buzz => CommandUrn<Switch>.Build(Urn, "BUZZ");
        public PropertyUrn<Percentage> fan_speed => PropertyUrn<Percentage>.Build(Urn, nameof(fan_speed));
        public CommandNode<Percentage> _send => CommandNode<Percentage>.Create("SEND",this);
        public PropertyUrn<Percentage> variable => PropertyUrn<Percentage>.Build(Urn, "variable");
        public PropertyUrn<FunctionDefinition> compute_constant => PropertyUrn<FunctionDefinition>.Build(Urn, "compute_constant");
    }


   
    
    public class fan: ModelNode
    {
        public CommandUrn<Percentage> _throttle => CommandUrn<Percentage>.Build(Urn, "THROTTLE");

        public fan(ModelNode parent) : base(nameof(fan), parent)
        {
        }
    }

    public class fan2:ModelNode
    {
        public CommandUrn<Percentage> _throttle => CommandUrn<Percentage>.Build(Urn, "THROTTLE");

        public fan2(ModelNode parent) : base(nameof(fan2), parent)
        {
        }
    }

    public class led : ModelNode
    {
        public CommandUrn<Switch> _switch => CommandUrn<Switch>.Build(Urn, "SWITCH");

        public led(ModelNode parent) : base(nameof(led), parent)
        {
        }
    }
}