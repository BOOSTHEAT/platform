using System;
using System.Collections.Generic;
using System.Linq;

namespace ImpliciX.Motors.Controllers.Domain
{
    public readonly struct MotorResponse
    {
        private MotorResponse(MotorIds motorId, Dictionary<MotorReadRegisters, string> registers)
        {
            MotorId = motorId;
            Registers = registers;
        }

        public MotorIds MotorId { get; }
        public Dictionary<MotorReadRegisters, string> Registers { get; }

        public static Func<Dictionary<MotorReadRegisters, string>, MotorResponse> Create(MotorIds motorId) =>
            registers => new MotorResponse(motorId, registers);

        public static Func<byte[], MotorResponse> CreateEmpty(MotorIds motorId) =>
            _ => new MotorResponse(motorId, new Dictionary<MotorReadRegisters, string>());

        public override string ToString() => 
            string.Join(";", Registers.Select(kvp => $"{kvp.Key}={kvp.Value}"));
    }

    [Serializable]
    public class MotorException : Exception
    {
        public MotorResponse Response { get; }

        public MotorException(MotorResponse response)
        {
            Response = response;
        }
    }
}