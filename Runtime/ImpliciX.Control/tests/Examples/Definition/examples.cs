using ImpliciX.Language.Model;

namespace ImpliciX.Control.Tests.Examples.Definition
{
    public class examples : RootModelNode
    {
        public examples() : base(nameof(examples))
        {
        }

        public static include_fragment include_fragment => new include_fragment(new examples());
        public static test_fragment test_fragment => new test_fragment(new examples());
        public static test_inner_fragment test_inner_fragment => new test_inner_fragment(new examples());
        public static nested_composites nested_composites => new nested_composites(new examples());
        public static complex_subsystem complex_subsystem => new complex_subsystem(new examples());
        public static simplified_subsystem simplified_subsystem => new simplified_subsystem(new examples());
        public static subsystem_a subsystem_a => new subsystem_a(new examples());
        public static subsystem_b subsystem_b => new subsystem_b(new examples());
        public static PropertyUrn<Percentage> dummy => PropertyUrn<Percentage>.Build(nameof(examples), nameof(dummy));
        public static PropertyUrn<Percentage> percentage => PropertyUrn<Percentage>.Build(nameof(examples), nameof(percentage));
        public static always always => new always(new examples());
        public static subsystemWithPeriodicComputation subsystemWithPeriodicComputation => new subsystemWithPeriodicComputation(new examples());
        public static timeout_subsystem timeout_subsystem => new timeout_subsystem(nameof(timeout_subsystem), new examples());
        public static timeout_subsystem timeout_subsystem_a => new timeout_subsystem(nameof(timeout_subsystem_a), new examples());
        public static timeout_subsystem timeout_subsystem_b => new timeout_subsystem(nameof(timeout_subsystem_b), new examples());
    }

    public class include_fragment : SubSystemNode
    {
        public include_fragment(ModelNode parent) : base(nameof(include_fragment), parent)
        {
        }

        public PropertyUrn<SubsystemState> public_state => PropertyUrn<SubsystemState>.Build(Urn, nameof(public_state));
        public CommandUrn<NoArg> _toBc => CommandUrn<NoArg>.Build(Urn, "TOBC");
    }

    public class test_fragment : SubSystemNode
    {
        public test_fragment(ModelNode parent) : base(nameof(test_fragment), parent)
        {
        }
    }

    public class test_inner_fragment : SubSystemNode
    {
        public test_inner_fragment(ModelNode parent) : base(nameof(test_inner_fragment), parent)
        {
        }
    }

    public class subsystemWithPeriodicComputation : SubSystemNode
    {
        public subsystemWithPeriodicComputation(ModelNode parent) : base(nameof(subsystemWithPeriodicComputation), parent)
        {
        }

        public PropertyUrn<Temperature> propB => PropertyUrn<Temperature>.Build(Urn, nameof(propB));
        public PropertyUrn<Temperature> propA => PropertyUrn<Temperature>.Build(Urn, nameof(propA));
        public PropertyUrn<Temperature> initialValue => PropertyUrn<Temperature>.Build(Urn, nameof(initialValue));
        public PropertyUrn<FunctionDefinition> functionDefinition => PropertyUrn<FunctionDefinition>.Build(Urn, "functionDefinition");
    }

    public class always : SubSystemNode
    {
        public always(ModelNode parent) : base(nameof(always), parent)
        {
        }

        public PropertyUrn<Literal> xprop => PropertyUrn<Literal>.Build(Urn, nameof(xprop));
        public PropertyUrn<Literal> yprop => PropertyUrn<Literal>.Build(Urn, nameof(yprop));
        public PropertyUrn<Literal> yprop_default => PropertyUrn<Literal>.Build(Urn, nameof(yprop_default));
        public PropertyUrn<Temperature> propA => PropertyUrn<Temperature>.Build(Urn, nameof(propA));
        public PropertyUrn<Temperature> propC => PropertyUrn<Temperature>.Build(Urn, nameof(propC));
        public PropertyUrn<Temperature> prop25 => PropertyUrn<Temperature>.Build(Urn, nameof(prop25));
        public PropertyUrn<Temperature> prop100 => PropertyUrn<Temperature>.Build(Urn, nameof(prop100));
        public CommandUrn<NoArg> _toB => CommandUrn<NoArg>.Build(Urn, "TOB");
        public CommandUrn<NoArg> _toAb => CommandUrn<NoArg>.Build(Urn, "TOAB");
        public CommandUrn<NoArg> _toAa => CommandUrn<NoArg>.Build(Urn, "TOAA");
        public PropertyUrn<AlwaysSubsystem.PublicState> always_public_state => PropertyUrn<AlwaysSubsystem.PublicState>.Build(Urn, nameof(always_public_state));
        public PropertyUrn<Percentage> zprop => PropertyUrn<Percentage>.Build(Urn, nameof(zprop));
        public PropertyUrn<Percentage> tprop => PropertyUrn<Percentage>.Build(Urn, nameof(tprop));
        public PropertyUrn<FunctionDefinition> func => PropertyUrn<FunctionDefinition>.Build(Urn, nameof(func));
    }

    public class complex_subsystem : SubSystemNode
    {
        public complex_subsystem(ModelNode parent) : base(nameof(complex_subsystem), parent)
        {
        }

        public CommandUrn<NoArg> _tab => CommandUrn<NoArg>.Build(Urn, "TAb");
        public CommandUrn<NoArg> _tb => CommandUrn<NoArg>.Build(Urn, "TB");
        public CommandUrn<NoArg> _tc => CommandUrn<NoArg>.Build(Urn, "TC");
        public CommandUrn<NoArg> _td => CommandUrn<NoArg>.Build(Urn, "Td");
        public CommandUrn<NoArg> _ta => CommandUrn<NoArg>.Build(Urn, "TA");
        public PropertyUrn<PowerSupply> prop1 => PropertyUrn<PowerSupply>.Build(Urn, nameof(prop1));
        public CommandUrn<PowerSupply> _te => CommandUrn<PowerSupply>.Build(Urn, "TE");
        public CommandUrn<NoArg> _tg => CommandUrn<NoArg>.Build(Urn, "TG");
        public CommandUrn<Literal> _cmd1 => CommandUrn<Literal>.Build(Urn, "CMD1");
        public PropertyUrn<PowerSupply> prop2 => PropertyUrn<PowerSupply>.Build(Urn, nameof(prop2));
        public PropertyUrn<PowerSupply> prop3 => PropertyUrn<PowerSupply>.Build(Urn, nameof(prop3));
    }

    public class simplified_subsystem : SubSystemApiNode
    {
        public simplified_subsystem(ModelNode parent) : base(nameof(simplified_subsystem), parent)
        {
        }

        public PropertyUrn<Presence> presence => PropertyUrn<Presence>.Build(Urn, nameof(presence));
    }

    public class subsystem_a : SubSystemNode
    {
        public subsystem_a(ModelNode parent) : base(nameof(subsystem_a), parent)
        {
        }

        public PropertyUrn<Percentage> needs => PropertyUrn<Percentage>.Build(nameof(examples), nameof(needs));
        public PropertyUrn<Percentage> threshold => PropertyUrn<Percentage>.Build(nameof(examples), nameof(threshold));
    }

    public class subsystem_b : SubSystemNode
    {
        [ValueObject]
        public enum SyncState
        {
            StartRequested,
            Started,
            StopRequested,
            Stopped
        }
        
        public subsystem_b(ModelNode parent) : base(nameof(subsystem_b), parent)
        {
        }

        public CommandUrn<NoArg> _start => CommandUrn<NoArg>.Build(Urn, "START");
        public CommandUrn<NoArg> _stop => CommandUrn<NoArg>.Build(Urn, "STOP");
        public PropertyUrn<SyncState> sync_state => PropertyUrn<SyncState>.Build(Urn, nameof(sync_state));
    }

    public class nested_composites : SubSystemNode
    {
        public nested_composites(ModelNode parent) : base(nameof(nested_composites), parent)
        {
        }

        public CommandUrn<NoArg> _tb => CommandUrn<NoArg>.Build(Urn, "TB");
        public CommandUrn<NoArg> _tbab => CommandUrn<NoArg>.Build(Urn, "TBAB");
        public CommandUrn<NoArg> _tbb => CommandUrn<NoArg>.Build(Urn, "TBB");
        public CommandUrn<Literal> _cmd1 => CommandUrn<Literal>.Build(Urn, "CMD1");
        public CommandUrn<Literal> _cmd2 => CommandUrn<Literal>.Build(Urn, "CMD2");
        public PropertyUrn<Literal> value => PropertyUrn<Literal>.Build(Urn, "Value");
    }

    public class timeout_subsystem : SubSystemNode
    {
        public timeout_subsystem(string name, ModelNode parent) : base(name, parent)
        {
        }

        public PropertyUrn<Duration> timeoutUrn = PropertyUrn<Duration>.Build(Urn.BuildUrn(nameof(timeoutUrn)));
        public CommandUrn<NoArg> toggle => CommandUrn<NoArg>.Build(Urn, "TOGGLE");
    }
}