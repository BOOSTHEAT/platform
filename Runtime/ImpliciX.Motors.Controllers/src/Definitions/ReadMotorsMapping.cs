using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Domain;

namespace ImpliciX.Motors.Controllers.Definitions
{
    
    public class RegistersMap
    {
        public List<ReadDefinition> MotorMapDefinitions { get; }


        public RegistersMap(Motor[] allMotors, List<ReadDefinition> readDefinitions=null)
        {
            MotorMapDefinitions = readDefinitions ?? GetMotorMapDefinitions(allMotors);
        }


        public Result<IList<IDataModelValue>> Eval(Dictionary<MotorIds, MotorResponse> motorResponses, TimeSpan at)
        {
            var dataModelValues = new List<IDataModelValue>();
            foreach (var definition in MotorMapDefinitions)
            {
                var result = definition.decodeFunction(motorResponses[definition.motorId].Registers[definition.register], at);
                if (result.IsError) return result.Error;
                dataModelValues.AddRange(result.Value);
            }
            return dataModelValues;   
        }

        public IEnumerable<IDataModelValue> EvalAllFailed(TimeSpan at) =>
            from definition in MotorMapDefinitions
            select Property<MeasureStatus>.Create(definition.statusUrn, MeasureStatus.Failure, at);


        private Func<T, TimeSpan, Result<IDataModelValue[]>> CreateDataModelValue<T>(PropertyUrn<T> measureUrn, PropertyUrn<MeasureStatus> statusUrn) =>
            (value,at) =>
                new IDataModelValue[]
                {
                    Property<T>.Create(measureUrn, value, at),
                    Property<MeasureStatus>.Create(statusUrn, MeasureStatus.Success, at),
                };
        
        private List<ReadDefinition> GetMotorMapDefinitions(Motor[] allMotors)
        {
            var definition = new List<ReadDefinition>();
            foreach (var (motorId, motor) in allMotors)
            {
                definition.AddRange(new[]
                {
                    new ReadDefinition()
                    {
                        motorId = motorId,
                        register = MotorReadRegisters.BSP,
                        statusUrn = motor.mean_speed.status,
                        decodeFunction = (s,at) => ToRotationalSpeed(s).SelectMany(f => CreateDataModelValue(
                                motor.mean_speed.measure,
                                motor.mean_speed.status)(RotationalSpeed.FromFloat(f).Value,at)
                        )
                    },
                    new ReadDefinition()
                    {
                        motorId = motorId,
                        register = MotorReadRegisters.DTE,
                        statusUrn = motor.temperature.status,
                        decodeFunction = (s,at) => TenthOfDegreeToKelvin(s).SelectMany(f => CreateDataModelValue(
                                motor.temperature.measure,
                                motor.temperature.status)(Temperature.Create(f), at)
                        )
                    },
                    new ReadDefinition()
                    {
                        motorId = motorId,
                        register = MotorReadRegisters.BPO,
                        statusUrn = motor.mechanical_power.status,
                        decodeFunction = (s,at) => ToWatt(s).SelectMany(f => CreateDataModelValue(
                                motor.mechanical_power.measure,
                                motor.mechanical_power.status)(Power.FromFloat(f).Value,at)
                        )
                    },
                    new ReadDefinition()
                    {
                        motorId = motorId,
                        register = MotorReadRegisters.OCU,
                        statusUrn = motor.current.status,
                        decodeFunction = (s,at) => 
                            MilliAmpereToAmpere(s).SelectMany(f => CreateDataModelValue(
                                motor.current.measure,
                                motor.current.status)(Current.FromFloat(f).Value, at)
                        )
                    },
                    new ReadDefinition()
                    {
                        motorId = motorId,
                        register = MotorReadRegisters.SVO,
                        statusUrn = motor.supply_voltage.status,
                        decodeFunction = (s, at) => MilliVoltToVolt(s).SelectMany(f => CreateDataModelValue(
                                motor.supply_voltage.measure,
                                motor.supply_voltage.status)(Voltage.FromFloat(f).Value, at)
                        )
                    },
                    new ReadDefinition()
                    {
                        motorId = motorId,
                        register = MotorReadRegisters.STA,
                        statusUrn = motor.voltage_operational_state.status,
                        decodeFunction = (s,at) =>
                            ToVoltageOperationalState(s).SelectMany(motorCurrent => CreateDataModelValue(
                                    motor.voltage_operational_state.measure,
                                    motor.voltage_operational_state.status)(motorCurrent,at)
                            )
                    },
                    new ReadDefinition()
                    {
                        motorId = motorId,
                        register = MotorReadRegisters.STA,
                        statusUrn = motor.current_operational_state.status,
                        decodeFunction = (s,at) =>
                            ToCurrentOperationalState(s).SelectMany(motorCurrent => CreateDataModelValue(
                                    motor.current_operational_state.measure,
                                    motor.current_operational_state.status)(motorCurrent,at)
                            )
                    },
                });
            }

            return definition;
        }

        public static Result<float> ToRotationalSpeed(string s) => TryParseFloat(s);
        public static Result<float> ToWatt(string s) => TryParseFloat(s);

        public static Result<float> TenthOfDegreeToKelvin(string s) =>
            TryParseFloat(s).Select(f => f / 10 + 273.15f);

        public static Result<float> MilliAmpereToAmpere(string s) => TryParseFloat(s).Select(f => f / 1000);
        public static Result<float> MilliVoltToVolt(string s) => TryParseFloat(s).Select(f => f / 1000);

        public static Result<MotorCurrent> ToCurrentOperationalState(string s) => TryParseInt(s)
            .SelectMany<int, MotorCurrent>(i => ((i & 0x38) >> 3) switch
            {
                0b000 => MotorCurrent.Normal,
                0b001 => MotorCurrent.DeRating,
                0b010 => MotorCurrent.SoftwareDisjunction,
                0b011 => MotorCurrent.SoftwareDisjunction,
                0b100 => MotorCurrent.HardwareDisjunction,
                0b101 => MotorCurrent.HardwareDisjunction,
                0b110 => MotorCurrent.HardwareDisjunction,
                0b111 => MotorCurrent.HardwareDisjunction,
                _ => new Error("", "")
            });

        public static Result<MotorVoltage> ToVoltageOperationalState(string s) => TryParseInt(s)
            .SelectMany<int, MotorVoltage>(i => ((i & 0x07)) switch
            {
                0b000 => MotorVoltage.Normal,
                0b001 => MotorVoltage.UnderVoltage,
                0b010 => MotorVoltage.OverVoltage,
                0b011 => MotorVoltage.OverVoltage,
                0b100 => MotorVoltage.BrakingLimitation,
                0b101 => MotorVoltage.BrakingLimitation,
                0b110 => MotorVoltage.OverVoltage,
                0b111 => MotorVoltage.OverVoltage,
                _ => new Error("", "")
            });

        private static Result<float> TryParseFloat(string value)
        {
            try
            {
                return Result<float>.Create(float.Parse(value));
            }
            catch (Exception)
            {
                return Result<float>.Create(new Error("", ""));
            }
        }

        private static Result<int> TryParseInt(string value)
        {
            try
            {
                return Result<int>.Create(int.Parse(value));
            }
            catch (Exception)
            {
                return Result<int>.Create(new Error("", ""));
            }
        }
    }

    public struct ReadDefinition
    {
        public MotorIds motorId { get; set; }
        public PropertyUrn<MeasureStatus> statusUrn { get; set; }
        public MotorReadRegisters register { get; set; }
        public Func<string, TimeSpan, Result<IDataModelValue[]>> decodeFunction { get; set; }
    }
}