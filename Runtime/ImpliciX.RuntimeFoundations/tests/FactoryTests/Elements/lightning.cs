using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects;

namespace ImpliciX.RuntimeFoundations.Tests.FactoryTests.Elements
{
    public class lightning:RootModelNode
    {
        public lightning() : base(nameof(lightning))
        {
        }

        static lightning()
        {
            interior = new interior(new lightning());
        }
        
        public static interior interior { get; } 
    }

    public class interior : ModelNode
    {
        public interior(ModelNode parent) : base(nameof(interior), parent)
        {
            _shutdown = CommandUrn<NoArg>.Build(Urn,"SHUTDOWN");
            _restart = CommandUrn<NoArg>.Build(Urn,"RESTART");
            kitchen =  new kitchen(this);
        }

        public CommandUrn<NoArg> _shutdown { get;  }
        public CommandUrn<NoArg> _restart { get; }

        public kitchen kitchen { get;  }
    }

    public class kitchen:ModelNode
    {
        public kitchen(ModelNode parent) : base(nameof(kitchen), parent)
        {
            _clean = CommandNode<Target>.Create("CLEAN",this);
            _switch = CommandUrn<Switch>.Build(Urn, "SWITCH");
            _tune = CommandUrn<Intensity>.Build(Urn, "TUNE");
            consumption = PropertyUrn<PowerConsumption>.Build(Urn, nameof(consumption));
            compute = PropertyUrn<FunctionDefinition>.Build(Urn, "compute");
            lights = new lights_node(this);
            settings = new settings_node(this);
        }
        public CommandNode<Target> _clean { get; }
        public CommandUrn<Switch> _switch  { get; } 
        public CommandUrn<Intensity> _tune  { get; } 
        public PropertyUrn<PowerConsumption> consumption  { get; }
        public PropertyUrn<FunctionDefinition> compute { get; }

        public lights_node lights { get; }
        public settings_node settings { get; }

        public class settings_node : ModelNode
        {
            public settings_node(ModelNode parent) : base(nameof(kitchen.settings), parent)
            {
                mode = UserSettingUrn<ControlMode>.Build(Urn, nameof(mode));
                defaultMode = FactorySettingUrn<ControlMode>.Build(Urn, nameof(defaultMode));
                coef = VersionSettingUrn<Percentage>.Build(Urn, nameof(coef));
            }
            public UserSettingUrn<ControlMode> mode { get; }
            public FactorySettingUrn<ControlMode> defaultMode { get; }
            public VersionSettingUrn<Percentage> coef { get; }
        }

        public class lights_node : ModelNode
        {
            public lights_node(ModelNode parent) : base(nameof(lights), parent)
            {
                _1  = new light_node("1",this);
                _2 = new light_node("2",this);
            }

            public light_node _1 { get; }
            public light_node _2 { get; }

        }
        
        public class light_node:ModelNode
        {
            public light_node(string urnToken, ModelNode parent) : base(urnToken, parent)
            {
                status = PropertyUrn<LightStatus>.Build(Urn,nameof(status));
            }
            
            public  PropertyUrn<LightStatus> status { get; }
        }
    }

    
}