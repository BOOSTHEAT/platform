using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Motors.Controllers.Domain;

namespace ImpliciX.Motors.Controllers.Tests
{
    public static class MotorsTestsUtils
    {
        public static Dictionary<MotorReadRegisters, string> CreateRegistersDictionary(string register, string value)
        {
            return new Dictionary<MotorReadRegisters, string>() {{Enum.Parse<MotorReadRegisters>(register), value}};
        }

        public static Dictionary<MotorReadRegisters, string> CreateRegistersDictionary(MotorReadRegisters register, string value)
        {
            return new Dictionary<MotorReadRegisters, string>() {{register, value}};
        }

        public static Dictionary<MotorReadRegisters, string> CreateRegistersDictionary(IEnumerable<(MotorReadRegisters motorRegister, string value)> registers)
        {
            return registers.ToDictionary(tuple => tuple.motorRegister, tuple => tuple.value);
        }
    }
}