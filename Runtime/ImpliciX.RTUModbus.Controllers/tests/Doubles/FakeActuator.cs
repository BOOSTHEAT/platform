using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    [ValueObject]
    public enum Position
    {
        A,
        B,
    }

    public class FakeActuator
    {
        private readonly ushort _switchToRegister;

        internal FakeActuator(ushort switchToRegister)
        {
            _switchToRegister = switchToRegister;
        }

        internal Result<Command> SwitchToStateless(object arg, TimeSpan _, IDriverState __) =>
            from position in SideEffect.SafeCast<Position>(arg)
            let cmd = position switch
            {
                Position.A => Command.Create(_switchToRegister, new ushort[] {0}),
                Position.B => Command.Create(_switchToRegister, new ushort[] {1}),
                _ => throw new NotImplementedException()
            }
            select cmd;
        
        
        internal Result<Command> SwitchToStateful(object arg, TimeSpan _, IDriverState state) =>
            from position in SideEffect.SafeCast<Position>(arg)
            let currentState = state.WithValue("last_position",position)
            let cmd = position switch
            {
                Position.A => Command.Create(_switchToRegister, new ushort[] {0}),
                Position.B => Command.Create(_switchToRegister, new ushort[] {1}),
                _ => throw new NotImplementedException()
            }
            select cmd.WithState(currentState);
    }
}