using System;
using System.Collections.Generic;
using System.Linq;

namespace ImpliciX.Motors.Controllers.Domain
{
    public enum MotorIds
    {
        M1,
        M2,
        M3
    }

    public enum MotorReadRegisters
    {
        BSP,
        BPO,
        DTE,
        OCU,
        SVO,
        STA
    }

    public enum MotorWriteRegisters
    {
        BFA
    }

    public static class MotorsUtils
    {
        public static IEnumerable<MotorIds> AllMotors() => All<MotorIds>();

        public static IEnumerable<MotorReadRegisters> AllMotorRegisters() => All<MotorReadRegisters>();

        public static string Address(this MotorIds @this) => @this switch
        {
            MotorIds.M1 => "01",
            MotorIds.M2 => "02",
            MotorIds.M3 => "03",
            _ => throw new NotSupportedException()
        };

        public static string Register(this MotorReadRegisters @this) => @this switch
        {
            MotorReadRegisters.BSP => nameof(MotorReadRegisters.BSP),
            MotorReadRegisters.BPO => nameof(MotorReadRegisters.BPO),
            MotorReadRegisters.DTE => nameof(MotorReadRegisters.DTE),
            MotorReadRegisters.OCU => nameof(MotorReadRegisters.OCU),
            MotorReadRegisters.SVO => nameof(MotorReadRegisters.SVO),
            MotorReadRegisters.STA => nameof(MotorReadRegisters.STA),
            _ => throw new NotSupportedException()
        };

        private static IEnumerable<A> All<A>() => Enum.GetValues(typeof(A)).Cast<A>();
    }
}