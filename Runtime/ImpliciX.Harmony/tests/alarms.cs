using ImpliciX.Language.Model;

namespace ImpliciX.Harmony.Tests
{
    internal class alarms : RootModelNode
    {
        private alarms() : base(nameof(alarms))
        {
        }

        static alarms()
        {
            C061 = new AlarmNode(nameof(C061), new alarms());
        }

        public static AlarmNode C061 { get; }
    }

    internal class fake_model : RootModelNode
    {
        private fake_model() : base(nameof(fake_model))
        {
        }

        static fake_model()
        {
            fake_litteral = PropertyUrn<Literal>.Build(nameof(fake_model), nameof(fake_litteral));
        }
        
        public static PropertyUrn<Literal> fake_litteral;
    }
}