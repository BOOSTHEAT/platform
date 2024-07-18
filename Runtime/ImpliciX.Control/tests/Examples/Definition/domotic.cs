using ImpliciX.Control.Tests.Examples.ValueObjects;
using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples.Definition
{
    public class domotic : RootModelNode
    {
        public domotic() : base(nameof(domotic))
        {
        }

        public static automatic_store automatic_store => new automatic_store(new domotic());
        public static secondary_store secondary_store => new secondary_store(new domotic());
        public static fancontroller fancontroller => new fancontroller(new domotic());
        public static connected_fan_master connected_fan_master => new connected_fan_master(new domotic());
        public static connected_fan_slave connected_fan_slave => new connected_fan_slave(new domotic());
       
    }

    public class connected_fan_slave : SubSystemNode
    {
        public connected_fan_slave(ModelNode parent) : base(nameof(connected_fan_slave), parent)
        {
        }

        public CommandUrn<NoArg> _start => CommandUrn<NoArg>.Build(Urn, "START");
        public CommandUrn<NoArg> _stop => CommandUrn<NoArg>.Build(Urn, "STOP");
    }


    public class connected_fan_master : SubSystemNode
    {
        public connected_fan_master(ModelNode parent) : base(nameof(connected_fan_master), parent)
        {
        }

        public CommandUrn<NoArg> _start => CommandUrn<NoArg>.Build(Urn, "START");
        public CommandUrn<NoArg> _stop => CommandUrn<NoArg>.Build(Urn, "STOP");
    }

    public class secondary_store : SubSystemNode
    {
        public secondary_store(ModelNode parent) : base(nameof(secondary_store), parent)
        {
        }

        public CommandUrn<NoArg> _open => CommandUrn<NoArg>.Build(Urn, "OPEN");
        public CommandUrn<NoArg> _close => CommandUrn<NoArg>.Build(Urn, "CLOSE");
        public CommandUrn<HowMuch> _closeWithParam => CommandUrn<HowMuch>.Build(Urn, "CLOSEWITHPARAM");
    }

    public class automatic_store : SubSystemNode
    {
        public automatic_store(ModelNode parent) : base(nameof(automatic_store), parent)
        {
        }

        public CommandUrn<NoArg> _close => CommandUrn<NoArg>.Build(Urn, "CLOSE");
        public CommandUrn<NoArg> _closed => CommandUrn<NoArg>.Build(Urn, "CLOSED");
        public CommandUrn<NoArg> _toDriver => CommandUrn<NoArg>.Build(Urn, "TODRIVER");
        public CommandUrn<Position> _open => CommandUrn<Position>.Build(Urn, "OPEN");
        public PropertyUrn<Duration> Timer => PropertyUrn<Duration>.Build(Urn, "TIMER");

        public light light => new light(this);
        public light_settings light_settings => new light_settings(this);
    }

    public class fancontroller : SubSystemNode
    {
        public fancontroller(ModelNode parent) : base(nameof(fancontroller), parent)
        {
        }

        public thermometer thermometer => new thermometer(this);

        public fan3 fan3 => new fan3(this);

        public CommandUrn<NoArg> _start => CommandUrn<NoArg>.Build(Urn, "START");
        public CommandUrn<NoArg> _stop => CommandUrn<NoArg>.Build(Urn, "STOP");
        public CommandUrn<NoArg> _stabilized => CommandUrn<NoArg>.Build(Urn, "STABILIZED");
        public CommandUrn<NoArg> _beforeStop => CommandUrn<NoArg>.Build(Urn, "BEFORESTOP");
        public PropertyUrn<FunctionDefinition> compute_throttle_polynomial => PropertyUrn<FunctionDefinition>.Build(Urn, "compute_throttle_polynomial");
        public PropertyUrn<FunctionDefinition> compute_throttle_pid => PropertyUrn<FunctionDefinition>.Build(Urn, "compute_throttle_pid");
        public PropertyUrn<Temperature> setpoint_temperature => PropertyUrn<Temperature>.Build(Urn, "setpoint_temperature");
        public PropertyUrn<Temperature> delta => PropertyUrn<Temperature>.Build(Urn, "delta");
        public PropertyUrn<Temperature> starting_temperature => PropertyUrn<Temperature>.Build(Urn, "starting_temperature");
    }

    public class fan3 : ModelNode
    {
        public fan3(ModelNode parent) : base(nameof(fan3), parent)
        {
        }

        public CommandNode<Temperature> _throttle => CommandNode<Temperature>.Create("THROTTLE", this);
        public PropertyUrn<Temperature> threshold => PropertyUrn<Temperature>.Build(Urn, "threshold");
    }

    public class thermometer : ModelNode
    {
        public PropertyUrn<Temperature> temperature => PropertyUrn<Temperature>.Build(Urn, "temperature");

        public thermometer(ModelNode parent) : base(nameof(thermometer), parent)
        {
        }
    }

    public class light : SubSystemNode
    {
        public light(ModelNode parent) : base(nameof(light), parent)
        {
        }


        public CommandUrn<Percentage> _intensity => CommandUrn<Percentage>.Build(Urn, "INTENSITY");

        //public CommandUrn<Switch> _switch => CommandUrn<Switch>.Build(Urn, "SWITCH");
        public CommandNode<Switch> _switch => CommandNode<Switch>.Create("SWITCH", this);
        public CommandUrn<LightColor> _change_color => CommandUrn<LightColor>.Build(Urn, "Change_Color");
    }

    public class light_settings : ModelNode
    {
        public PropertyUrn<Percentage> intensity => PropertyUrn<Percentage>.Build(Urn, "INTENSITY");

        public PropertyUrn<Percentage> default_intensity =>
            PropertyUrn<Percentage>.Build(Urn, nameof(default_intensity));
        public PropertyUrn<LightColor> default_color => PropertyUrn<LightColor>.Build(Urn, "White");
        public PropertyUrn<LightColor> user_defined_color => PropertyUrn<LightColor>.Build(Urn, "Blue");


        public light_settings(ModelNode parent) : base(nameof(light_settings), parent)
        {
        }
    }
}