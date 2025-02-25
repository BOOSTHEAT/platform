using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    public class fake_urn : RootModelNode
    {
        static fake_urn()
        {
            energy = new MeasureNode<Energy>(nameof(energy), new fake_urn());
            volume = new MeasureNode<Volume>(nameof(volume), new fake_urn());
            flow = new MeasureNode<Flow>(nameof(flow), new fake_urn());
            gasFlow = new MeasureNode<StandardLiterPerMinute>(nameof(gasFlow), new fake_urn());
            temperature1 = new MeasureNode<Temperature>(nameof(temperature1), new fake_urn());
            temperature2 = new MeasureNode<Temperature>(nameof(temperature2), new fake_urn());
            pressure1 = new MeasureNode<Pressure>(nameof(pressure1), new fake_urn());
            somePercentage = new MeasureNode<Percentage>(nameof(somePercentage), new fake_urn());
            reset_cause = new MeasureNode<int>(nameof(reset_cause), new fake_urn());
            version1 = new MeasureNode<SoftwareVersion>(nameof(version1), new fake_urn());
            version2 = new MeasureNode<SoftwareVersion>(nameof(version2), new fake_urn());
            _smurf = CommandNode<NoArg>.Create("SMURF", new fake_urn());
            _switch = CommandNode<Position>.Create("SWITCH", new fake_urn());
            _throttle = CommandNode<Percentage>.Create("THROTTLE", new fake_urn());
            _throttle2 = CommandNode<Percentage>.Create("THROTTLE2", new fake_urn());
            _setPoint = CommandNode<Temperature>.Create("SETPOINT", new fake_urn());
            _setPoint2 = CommandNode<Temperature>.Create("SETPOINT2", new fake_urn());
            _setPoint3 = CommandNode<Temperature>.Create("SETPOINT3", new fake_urn());
            mcu_fake1 = new HardwareAndSoftwareDeviceNode(nameof(mcu_fake1), new fake_urn());
            mcu_fake2 = new HardwareAndSoftwareDeviceNode(nameof(mcu_fake2), new fake_urn());
            board_state = new MeasureNode<BoardState>(nameof(board_state), new fake_urn());
            ignition_fault = new MeasureNode<Fault>(nameof(ignition_fault), new fake_urn());

        }
        
        private fake_urn() : base(nameof(fake_urn))
        {
        }

        public static MeasureNode<Energy> energy { get; }
        public static MeasureNode<Volume> volume { get; }
        public static MeasureNode<Flow> flow { get; }
        
        public static MeasureNode<Fault> ignition_fault { get; }
        public static MeasureNode<StandardLiterPerMinute> gasFlow { get; }
        public static MeasureNode<Temperature> temperature1 { get; }
        
        public static MeasureNode<Temperature> temperature2 { get; }
        
        public static MeasureNode<Pressure> pressure1 { get; }
        public static MeasureNode<Percentage> somePercentage { get; }
        public static MeasureNode<int> reset_cause { get;  }
        public static MeasureNode<SoftwareVersion> version1 { get; }
        public static MeasureNode<SoftwareVersion> version2 { get;  }
        public static CommandNode<NoArg> _smurf { get; }
        public static CommandNode<Position> _switch { get; }
        public static CommandNode<Percentage> _throttle { get; }
        public static CommandNode<Percentage> _throttle2 { get; }
        public static CommandNode<Temperature> _setPoint { get; }
        public static CommandNode<Temperature> _setPoint2 { get; }
        public static CommandNode<Temperature> _setPoint3 { get; }
        
        
        public static HardwareAndSoftwareDeviceNode mcu_fake1 { get; }
        public static HardwareAndSoftwareDeviceNode mcu_fake2 { get; }
        public static MeasureNode<BoardState> board_state { get; }
    }

   
}