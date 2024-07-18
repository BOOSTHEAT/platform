using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    public static class FakeDecoders
    {

        public static readonly MeasureDecoder ResetCause =
            (measureUrn, statusUrn, registers, currentTime, _) => 
                Measure<int>.Create(measureUrn, statusUrn, (int) registers[0], currentTime);
        
        public static readonly MeasureDecoder McuSoftwareVersion =
            (measureUrn, statusUrn, registers, currentTime,_) =>Measure<SoftwareVersion>.Create(measureUrn, statusUrn,
                SoftwareVersion.Create(registers[0], registers[1], registers[2], registers[3]), currentTime);

        
        public static MeasureDecoder ProbeDecode<T>(Func<float, Result<float>> converter,
            Func<float, Result<T>> valueObjectFactory) =>
            (measureUrn, statusUrn, registers, currentTime, _) =>
            {
                var modelObject =
                    from rawValue in converter(RegistersConverterHelper.ToFloatMswLast(registers))
                    from vo in valueObjectFactory(rawValue)
                    select vo;
                return Measure<T>.Create(measureUrn, statusUrn, modelObject, currentTime);
            };

        public static  MeasureDecoder SimulateErrorDecode<T>() =>
            (measureUrn, statusUrn, registers, currentTime, _) => Measure<T>.Create(measureUrn, statusUrn, new ProbeError(), currentTime);
        
        public static readonly MeasureDecoder MCU_BoardState
            = (measureUrn, statusUrn, registers, currentTime,_) =>
            {
                var value = RegisterToBoardState(registers);
                return Measure<BoardState>.Create(measureUrn, statusUrn, value, currentTime);
            };

        public static Result<BoardState> RegisterToBoardState(ushort[] registers)
        {
            return registers[0] switch
            {
                1 => BoardState.WaitingForStart,
                2 => BoardState.UpdateRunning,
                3 => BoardState.RegulationStarted,
                4 => BoardState.RegulationStarted,
                _ => Result<BoardState>.Create(new DecodeError($"Board state unknown"))
            };
        }
    }
}