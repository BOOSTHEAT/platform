using ImpliciX.Language.Model;

namespace ImpliciX.RTUModbus.Controllers.Tests.Model
{
    public class test_model : RootModelNode
    {
        static test_model()
        {
            software = new software(new test_model());
            measures = new measures(new test_model());
            commands = new commands(new test_model());
            burner = new TestBurner(new test_model()){}; 
            burner.burner_fan = new test_fan(burner);
            _commit_update = CommandNode<NoArg>.Create("COMMIT_UPDATE", new test_model());
            _rollback_update = CommandNode<NoArg>.Create("ROLLBACK_UPDATE", new test_model());
        }
        public static software software { get; }
        public static measures measures { get; }
        
        public static commands commands { get; }
        public static TestBurner burner { get; }

        public static CommandNode<NoArg> _commit_update { get; }
        public static CommandNode<NoArg> _rollback_update { get; }

        

        public test_model() : base(nameof(test_model))
        {
        }
        
       
    }
    public class burner<T> : BurnerNode where T:FanNode
    {

        public T fan
        {
            get => (T) burner_fan;
            set => burner_fan = value;
        }

        public burner(ModelNode parent) : base("burner",parent)
        {
        }
    }

    
    public class TestBurner : burner<test_fan>
    {
        public TestBurner(ModelNode parent) : base(parent)
        {
        }
    }
    
    public class test_fan : FanNode
    {
        public test_fan(ModelNode parent) : base(nameof(burner<test_fan>.fan), parent)
        {
        }
    }

    public class software : ModelNode
    {
        public software(ModelNode parent) : base(nameof(software), parent)
        {
            fake_daughter_board =  new HardwareAndSoftwareDeviceNode(nameof(fake_daughter_board), this);
            fake_other_board = new HardwareAndSoftwareDeviceNode(nameof(fake_other_board), this);
            
            _commit_update = CommandNode<NoArg>.Create("COMMIT_UPDATE", this);
            _rollback_update = CommandNode<NoArg>.Create("ROLLBACK_UPDATE", this);
            
        }
        
        public HardwareAndSoftwareDeviceNode fake_daughter_board { get; } 
        public HardwareAndSoftwareDeviceNode fake_other_board { get; } 
        
        public CommandNode<NoArg> _commit_update { get; }
        public CommandNode<NoArg> _rollback_update { get; }
        
        
       
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
            do_something_noarg = CommandNode<NoArg>.Create(nameof(do_something_noarg), this);
        }
        
        public CommandNode<Percentage> do_something { get; }
        public CommandNode<NoArg> do_something_noarg { get; }
        
    }
}