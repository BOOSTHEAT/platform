using ImpliciX.Language.Model;

namespace ImpliciX.Driver.Common.Tests.Buffer
{
    public class fake_urn : RootModelNode
    {
        static fake_urn()
        {
            _switch = CommandNode<Position>.Create("SWITCH", new fake_urn());
            _throttle = CommandNode<Percentage>.Create("THROTTLE", new fake_urn());
            _setPoint = CommandNode<Temperature>.Create("SETPOINT", new fake_urn());
            _setPoint2 = CommandNode<Temperature>.Create("SETPOINT2", new fake_urn());
            _setPoint3 = CommandNode<Temperature>.Create("SETPOINT3", new fake_urn());
        }
        private fake_urn() : base(nameof(fake_urn))
        {
        }
        
        public static CommandNode<Position> _switch { get; }
        public static CommandNode<Percentage> _throttle { get; }
        public static CommandNode<Temperature> _setPoint { get; }
        public static CommandNode<Temperature> _setPoint2 { get; }
        public static CommandNode<Temperature> _setPoint3 { get; }
    }

    [ValueObject]
    public enum Position
    {
        A,
        B,
    }
}