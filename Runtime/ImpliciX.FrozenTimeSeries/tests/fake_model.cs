using ImpliciX.Language.Model;

namespace ImpliciX.FrozenTimeSeries.Tests
{
    public class fake_model : RootModelNode
    {
        public enum PublicState
        {
            Running = 2,
            Disabled = 0
        }

        public enum PublicState2
        {
            Running = 2,
            Disabled = 0,
            Other = 3,
        }

        public fake_model() : base(nameof(fake_model))
        {
        }

        static fake_model()
        {
            var parent = new fake_model();
            public_state = PropertyUrn<PublicState>.Build("fake_model", nameof(public_state));
            public_state2 = PropertyUrn<PublicState2>.Build("fake_model", nameof(public_state2));
            notInDeclarations = PropertyUrn<Counter>.Build("fake_model", nameof(notInDeclarations));
            C666 = new AlarmNode(nameof(C666), parent);
            temperature = new fake_temperature(parent);
            fake_index = PropertyUrn<Flow>.Build("fake_model", nameof(fake_index));
            fake_index_again = PropertyUrn<Flow>.Build("fake_model", nameof(fake_index_again));
            supply_temperature = new supply_temperature(parent);

        }

        public static supply_temperature supply_temperature { get; }
        public static PropertyUrn<PublicState> public_state { get; }
        public static fake_temperature temperature { get; }
        public static PropertyUrn<Flow> fake_index { get; }
        public static PropertyUrn<Flow> fake_index_again { get; }
        public static PropertyUrn<PublicState2> public_state2 { get; }
        public static PropertyUrn<Counter> notInDeclarations { get; }
        public static AlarmNode C666 { get; }
    }

    public class fake_temperature : MeasureNode<Temperature>
    {
        public fake_temperature(ModelNode parent) : base(nameof(fake_temperature), parent)
        {
        }
    }
    
    public class supply_temperature : MeasureNode<Temperature>
    {
        public supply_temperature(ModelNode parent) : base(nameof(supply_temperature), parent)
        {
        }
    }
}