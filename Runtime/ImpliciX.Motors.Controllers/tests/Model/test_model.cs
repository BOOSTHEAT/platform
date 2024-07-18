using ImpliciX.Language.Model;

namespace ImpliciX.Motors.Controllers.Tests.Model
{
    public class test_model : RootModelNode
    {
        static test_model()
        {
            software = new software(new test_model());
            measures = new measures(new test_model());
            commands = new commands(new test_model());
            motors = new motors(new test_model());
        }
        public static software software { get; }
        public static measures measures { get; }
        
        public static commands commands { get; }

        public static motors motors { get; }
        public test_model() : base(nameof(test_model))
        {
        }
    }

    public class motors : ModelNode
    {
        public motors(ModelNode parent) : base(nameof(motors), parent)
        {
            _1 = new MotorNode("1", this);
            _2 = new MotorNode("2", this);
            _3 = new MotorNode("3", this);
            Nodes = new [] { _1, _2, _3 };
            supply_delay = PropertyUrn<Duration>.Build(Urn,nameof(supply_delay));
            status = PropertyUrn<MotorsStatus>.Build(Urn,nameof(status));
            _supply = CommandNode<PowerSupply>.Create("SUPPLY", this);
            _power = CommandNode<PowerSupply>.Create("POWER", this);
            _switch = CommandUrn<MotorStates>.Build(Urn, "SWITCH");
        }
        public MotorNode _1 { get; }
        public MotorNode _2 { get; }
        public MotorNode _3 { get; }
        public MotorNode[] Nodes { get; }
        public PropertyUrn<Duration> supply_delay { get; }
        public PropertyUrn<MotorsStatus> status { get; }
        public CommandNode<PowerSupply> _supply { get; }
        public CommandNode<PowerSupply> _power { get; }
        public CommandUrn<MotorStates> _switch { get; }
    }

    public class software : ModelNode
    {
        public software(ModelNode parent) : base(nameof(software), parent)
        {
            fake_motor_board =  new HardwareAndSoftwareDeviceNode(nameof(fake_motor_board), this);
            fake_heat_pump =  new HardwareAndSoftwareDeviceNode(nameof(fake_heat_pump), this);
            fake_eu =  new HardwareAndSoftwareDeviceNode(nameof(fake_eu), this);
        }
        
        public HardwareAndSoftwareDeviceNode fake_motor_board { get; }
        public HardwareAndSoftwareDeviceNode fake_heat_pump { get; }
        public HardwareAndSoftwareDeviceNode fake_eu { get; }
    }


    public class measures : ModelNode
    {
        public measures(ModelNode parent) : base(nameof(measures), parent)
        {
            temperature1 = new MeasureNode<Temperature>(nameof(temperature1), this);
            pressure1 = new MeasureNode<Pressure>(nameof(pressure1), this);
        }
        
        public MeasureNode<Temperature> temperature1 { get; }
        
        public MeasureNode<Pressure> pressure1 { get; }
    }
    
    
    public class commands : ModelNode
    {
        public commands(ModelNode parent) : base(nameof(commands), parent)
        {
            do_something = CommandNode<Percentage>.Create(nameof(do_something), this);
        }
        
        public CommandNode<Percentage> do_something { get; }
        
    }
}