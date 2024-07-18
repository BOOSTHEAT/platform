using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ImpliciX.Language.Core;
using ImpliciX.Motors.Controllers.Domain;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.SerialApi2.SerialPort;
using TimeoutSettings = ImpliciX.Motors.Controllers.Settings.TimeoutSettings;
using static ImpliciX.Language.Core.SideEffect;

namespace ImpliciX.Motors.Controllers.Infrastructure
{
    public static class SimpaApi2
    {
        public static byte STX = 0x02;
        public static byte ETX = 0x03;
        public static byte ACK = 0x06;
        public static byte NACK = 0x15;
        public static byte XONERROR = 0x17;
        public static byte XON = 0x1A;

        private static readonly string ReadPayload = ConstructPayloadFromReadMotorRegister(MotorsUtils.AllMotorRegisters());

        private const ushort PAYLOAD_START = 6;
        private const ushort SUFFIX_LENGTH = 4;
        private const ushort BUFFER_SIZE = 512;


        public static Result2<MotorResponse, CommunicationDetails> ReadMotor(MotorIds motor, IBhSerialPort serialPort, TimeoutSettings timeoutSettings)
        {
            var request = BuildReadRequest(motor);
                
            var readResult =
                from _ in Wait(timeoutSettings.ReadWriteDelay)
                from __ in WriteRequestFrame(serialPort, request, motor)
                from ___ in Wait(timeoutSettings.WriteReadDelay)
                from bytes in ReadResponseFrame(serialPort, motor)
                from response in ParseReadResponse(motor)(bytes)
                select response;

            return readResult.Match(
                err => Result2<MotorResponse, CommunicationDetails>.Create(err, new CommunicationDetails(0, 1)),
                response => Result2<MotorResponse, CommunicationDetails>.Create(response, new CommunicationDetails(1, 0))
            );
        }

        public static Result2<MotorResponse, CommunicationDetails> WriteMotor(MotorIds motor, float value, IBhSerialPort serialPort, TimeoutSettings timeoutSettings)
        {
            ushort failureCount = 0;
            var writeResult = 
                TryRun(
                    () => TryWrite(serialPort, timeoutSettings, value, motor).ThrowOnError(),
                    ex => new SimpaApiError(ex.Message),
                    SideEffect.RetryPolicy.Create(timeoutSettings.Retries),
                    (ex, tryNumber, totalRetries) => LogWriteException(ex, tryNumber, totalRetries)(motor, value),
                    _ => failureCount += 1
                ).UnWrap();

            return writeResult.Match(
                err => Result2<MotorResponse, CommunicationDetails>.Create(err, new CommunicationDetails(0, failureCount)),
                response => Result2<MotorResponse, CommunicationDetails>.Create(response, new CommunicationDetails(1, failureCount))
            );
        }


        private static Result<MotorResponse> TryWrite(IBhSerialPort serialPort, TimeoutSettings timeoutSettings, float value, MotorIds motor)
        {
            var convertedValue = Convert.ToInt32(value);
            var request = BuildWriteRequest(motor, convertedValue);
            
            return
                from _ in Wait(timeoutSettings.ReadWriteDelay) 
                from __ in WriteRequestFrame(serialPort, request, motor)
                from ___ in Wait(timeoutSettings.WriteReadDelay)
                from bytes in ReadResponseFrame(serialPort, motor)
                from response in ParseWriteResponse(motor)(bytes)
                select response;
          
         }
        
        private static Action<MotorIds, object> LogWriteException(Exception exception, int tryNumber, int totalRetries)
        {
            return (motor, arg) => 
                Log.Error(exception, "[{@Name}] [Try {@Try_Number}/{@Total_TryNumber}] Writing setpoint {@arg}. Message: {@message}",
                    motor,
                    tryNumber, totalRetries + 1,
                    arg,
                    exception.Message);
        }

        private static Result<Unit> WriteRequestFrame(IBhSerialPort serialPort, string request, MotorIds motor)
        {
            return TryRun(() =>
                {
                    var bytes = Encoding.ASCII.GetBytes(request);
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.Write(bytes,0,bytes.Length);
                return default(Unit);
            }, ex=>new SimpaApiError(ex.CascadeMessage()))
                .Tap(
                    error => Log.Error("Motor {@motor} error when writing request frame {@msg}",motor, error.Message),
                    _ => Log.Debug("Motor {@motor} request frame {@request}", motor, request));
            
        }

        public static Func<byte[], Result<MotorResponse>> ParseReadResponse(MotorIds motorId) => bytes =>
            CheckSimpaResponse(bytes)
                .SelectMany(CheckChecksum)
                .SelectMany(CheckMotorId(motorId))
                .SelectMany(ParseMotorRegisters)
                .Select(MotorResponse.Create(motorId))
                .Tap(
                    error => Log.Error("Motor {@motor} error when parsing frame {@frame}. {@message}",motorId,Encoding.ASCII.GetString(bytes), error.Message),
                    response => Log.Debug("Motor {@motor} parsed response {@response}", motorId, response.ToString()));

        public static Func<byte[], Result<MotorResponse>> ParseWriteResponse(MotorIds motorId) => bytes =>
            CheckSimpaResponse(bytes).Select(MotorResponse.CreateEmpty(motorId))
                .Tap(
                    error => Log.Error("Motor {@motor} error when parsing frame {@frame}. {@message}",motorId,Encoding.ASCII.GetString(bytes), error.Message),
                    response => Log.Debug("Motor {@motor} parsed response.", motorId));


        private static Result<byte[]> ReadResponseFrame(IBhSerialPort serialPort, MotorIds motor)
        {
            return ReadFrame().Tap(
                error =>Log.Error("Motor {@motor} error when reading response frame {@msg}",motor, error.Message),
                response => Log.Debug("Motor {@motor} response frame {@response}", motor, Encoding.ASCII.GetString(response)));

            Result<byte[]> ReadFrame()
            {
                return TryRun(() =>
                {
                    var buffer = new byte[BUFFER_SIZE];
                    var offset = 0;
                    while (true)
                    {
                        var lengthRead = serialPort.Read(buffer, offset, buffer.Length - offset);
                        offset += lengthRead;
                        if (buffer[offset - 1] == XON || buffer[offset - 1] == XONERROR)
                            return buffer[..offset];
                    }
                }, ex => new SimpaApiError(ex.CascadeMessage()));
            }
        }

        private static Result<byte[]> CheckSimpaResponse(byte[] buffer) =>
            buffer switch
            {
                var r when IsNack(r) => Result<byte[]>.Create(new NotAcknowledgedError()),
                var r when IsXonError(buffer.Length, r) => Result<byte[]>.Create(new XonError()),
                var r when IsAck(r) => Result<byte[]>.Create(buffer),
                _ => Result<byte[]>.Create(new UnknownResponseError())
            };

        private static Result<Dictionary<MotorReadRegisters, string>> ParseMotorRegisters(byte[] payload) =>
            StringFromBytes(payload).Split(",")
                .Select(str => str.Split("="))
                .ToDictionary(t => Enum.Parse<MotorReadRegisters>(t[0]), t => t[1]);

        private static Func<byte[], Result<byte[]>> CheckMotorId(MotorIds motorId) =>
            payload =>
            {
                if (StringFromBytes(payload[..2]) == motorId.Address())
                    return Result<byte[]>.Create(payload[2..]);
                else
                    return Result<byte[]>.Create(new WrongMotorIdResponseError());
            };

        private static Result<byte[]> CheckChecksum(byte[] message)
        {
            var payload = message[PAYLOAD_START..^SUFFIX_LENGTH];
            var checksum = message[^SUFFIX_LENGTH..^2];
            return Checksum(StringFromBytes(payload)) == StringFromBytes(checksum)
                ? Result<byte[]>.Create(payload)
                : new BadChecksumError();
        }

        public static string Checksum(string frameAddressAndPayload) =>
            (frameAddressAndPayload.Aggregate(0, (i, c) => i + c) % 256).ToString("X2");

        public static string WriteByteCount(string frameAddressAndPayload) =>
            frameAddressAndPayload.Length.ToString("D3");

        public static int ReadByteCount(string byteCount) =>
            100 * (byteCount[0] - 48) + 10 * (byteCount[1] - 48) + byteCount[2] - 48;

        private static string StringFromBytes(params byte[] bytes) => Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        private static bool IsXonError(int length, byte[] r) => r[length - 1] == XONERROR;
        private static bool IsNack(byte[] r) => r[0] == NACK;
        private static bool IsAck(byte[] r) => r[0] == ACK;

        private static string BuildReadRequest(MotorIds motorId)
        {
            var payload = $"{motorId.Address()}" + ReadPayload;
            var checksum = Checksum(payload);
            var byteCount = WriteByteCount(payload);
            return $"{b(STX)}{byteCount}{payload}{checksum}{b(ETX)}";
        }

        public static string ConstructPayloadFromReadMotorRegister(IEnumerable<MotorReadRegisters> motorRegisters)
        {
            var result = motorRegisters.Aggregate("", (acc, registers) => acc + "REA " + registers + ",");
            return result.Remove(result.Length - 1);
        }

        private static string BuildWriteRequest(MotorIds motorId, int value)
        {
            var payload = $"{motorId.Address()}{MotorWriteRegisters.BFA}={value}";
            var checksum = Checksum(payload);
            var byteCount = WriteByteCount(payload);
            return $"{b(STX)}{byteCount}{payload}{checksum}{b(ETX)}";
        }

        public static readonly Func<byte, string> b = e => Encoding.ASCII.GetString(new[] {e});

        private static Result<Unit> Wait(int delayInMs)
        {
            Thread.Sleep(delayInMs);
            return default(Unit);
        }
    }
}