using System;
using System.Collections.Generic;
using System.Text;
using ImpliciX.Language.Core;
using ImpliciX.Motors.Controllers.Domain;
using ImpliciX.Motors.Controllers.Infrastructure;
using ImpliciX.Motors.Controllers.Settings;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.SerialApi2.SerialPort;
using Moq;
using NFluent;
using NUnit.Framework;
using static ImpliciX.Motors.Controllers.Infrastructure.SimpaApi2;
using static ImpliciX.Motors.Controllers.Tests.MotorsTestsUtils;

namespace ImpliciX.Motors.Controllers.Tests
{
    [TestFixture]
    public class SimpaApiTests
    {
        [Test]
        public void checksum()
        {
            var address = "02";
            var payload = "MOVE_ON 123";
            var expected = "4B";
            var result = Checksum(address + payload);
            Check.That(result).Equals(expected);
        }

        [Test]
        public void write_bytecount()
        {
            var address = "01";
            var payload = "BFA=200";

            var expected = "009";
            var result = WriteByteCount(address + payload);

            Check.That(result).Equals(expected);
        }

        [TestCase("000", 0)]
        [TestCase("001", 1)]
        [TestCase("010", 10)]
        [TestCase("100", 100)]
        [TestCase("999", 999)]
        public void read_bytecount(string input, int expected)
        {
            Check.That(ReadByteCount(input)).Equals(expected);
        }

        private Dictionary<string, (Result<byte[]>, Result<MotorResponse>)> read_request_test_cases = new Dictionary<string, (Result<byte[]>, Result<MotorResponse>)>()
        {
            {
                "nominal",
                (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"{b(ACK)}\x42{b(STX)}01001BSP=+120{Checksum("01BSP=+120")}{b(ETX)}{b(XON)}")),
                    Result<MotorResponse>.Create(MotorResponse.Create(MotorIds.M1)(CreateRegistersDictionary(MotorReadRegisters.BSP, "+120"))))
            },
            {
                "stream closed",
                (Result<byte[]>.Create(new TimeOutError()), Result<MotorResponse>.Create(new TimeOutError()))
            },
            {
                "not acknowledged",
                (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"{b(NACK)}\x42{b(STX)}01001BSP=+120{Checksum("01BSP=+120")}{b(ETX)}{b(XON)}")), Result<MotorResponse>.Create(new NotAcknowledgedError()))
            },
            {
                "xonerror",
                (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"{b(ACK)}\x42{b(STX)}01001BSP=+120{Checksum("01BSP=+120")}{b(ETX)}{b(XONERROR)}")), Result<MotorResponse>.Create(new XonError()))
            },
            {
                "bad checksum",
                (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"{b(ACK)}\x42{b(STX)}01001BSP=+120{Checksum("01BSP=+120X")}{b(ETX)}{b(XON)}")), Result<MotorResponse>.Create(new BadChecksumError()))
            },
            {
                "unknown response",
                (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"\x48\x42{b(STX)}01001BSP=+120{Checksum("01BSP=+120")}{b(ETX)}{b(XON)}")), Result<MotorResponse>.Create(new UnknownResponseError()))
            },
            {
                "wrong motor id in response",
                (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"\x48\x42{b(STX)}01002BSP=+120{Checksum("02BSP=+120")}{b(ETX)}{b(XON)}")), Result<MotorResponse>.Create(new WrongMotorIdResponseError()))
            },
            {
                "multiple measures",
                (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"{b(ACK)}\x42{b(STX)}01001BSP=+120,DTE=-10,BPO=+25,SVO=+0{Checksum("01BSP=+120,DTE=-10,BPO=+25,SVO=+0")}{b(ETX)}{b(XON)}")),
                    Result<MotorResponse>.Create(MotorResponse.Create(MotorIds.M1)(  
                        CreateRegistersDictionary(new[] {(MotorReadRegisters.BSP, "+120"), (MotorReadRegisters.DTE, "-10"), (MotorReadRegisters.BPO, "+25"), (MotorReadRegisters.SVO, "+0")}))))
            },
        };

        [TestCase("nominal")]
        [TestCase("stream closed")]
        [TestCase("not acknowledged")]
        [TestCase("xonerror")]
        [TestCase("bad checksum")]
        [TestCase("unknown response")]
        [TestCase("multiple measures")]
        public void parse_read_request_response(string caseName)
        {
            var (result, expected) = read_request_test_cases[caseName];
            var motorResponse = result.SelectMany(ParseReadResponse(MotorIds.M1));
            motorResponse.Tap (
                error => Check.That(error).IsEqualTo(expected.Error),
                response => Check.That(response.Registers).IsEqualTo(expected.Value.Registers)
            );
        }

        [TestCase("nominal", MotorReadRegisters.BSP, "+120")]
        [TestCase("multiple measures", MotorReadRegisters.BSP, "+120")]
        [TestCase("multiple measures", MotorReadRegisters.DTE, "-10")]
        [TestCase("multiple measures", MotorReadRegisters.BPO, "+25")]
        [TestCase("multiple measures", MotorReadRegisters.SVO, "+0")]
        public void parse_read_response(string caseName, MotorReadRegisters expectedKey, string expectedValue)
        {
            var (input, _) = read_request_test_cases[caseName];
            Check.That(input.SelectMany(ParseReadResponse(MotorIds.M1)).Value.Registers[expectedKey]).Equals(expectedValue);
        }

        [Test]
        public void parse_read_request_response_when_response_empty()
        {
            var result = Result<byte[]>.Create(new TimeOutError());
            var returnedType = result.SelectMany(ParseReadResponse(MotorIds.M1)).Error.GetType();
            Check.That(returnedType).IsEqualTo(typeof(TimeOutError));
        }

        private Dictionary<string, (Result<byte[]>, bool, Type)> write_command_test_cases = new Dictionary<string, (Result<byte[]>, bool, Type)>()
        {
            {"nominal", (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"{b(ACK)}\x42{b(XON)}")), true, null)},
            {"stream closed", (Result<byte[]>.Create(new TimeOutError()), false, typeof(TimeOutError))},
            {"not acknowledged", (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"{b(NACK)}\x42{b(XON)}")), false, typeof(NotAcknowledgedError))},
            {"xonerror", (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"{b(ACK)}\x42{b(XONERROR)}")), false, typeof(XonError))},
            {"unknown response", (Result<byte[]>.Create(Encoding.ASCII.GetBytes($"\x48\x48\x48")), false, typeof(UnknownResponseError))}
        };

        [TestCase("nominal")]
        [TestCase("stream closed")]
        [TestCase("not acknowledged")]
        [TestCase("xonerror")]
        [TestCase("unknown response")]
        public void parse_write_command_response(string caseName)
        {
            var (input, expected, _) = write_command_test_cases[caseName];
            Check.That(input.SelectMany(ParseWriteResponse(MotorIds.M1)).IsSuccess).IsEqualTo(expected);
        }

        [TestCase("not acknowledged")]
        [TestCase("xonerror")]
        [TestCase("unknown response")]
        public void parse_write_command_response_return_the_correct_error(string caseName)
        {
            var (input, _, errorType) = write_command_test_cases[caseName];
            Check.That(input.SelectMany(ParseWriteResponse(MotorIds.M1)).Error.GetType()).IsEqualTo(errorType);
        }

        [TestCase(new[] {MotorReadRegisters.BPO}, "REA BPO")]
        [TestCase(new[] {MotorReadRegisters.BPO, MotorReadRegisters.BSP, MotorReadRegisters.DTE}, "REA BPO,REA BSP,REA DTE")]
        [TestCase(new[] {MotorReadRegisters.OCU}, "REA OCU")]
        public void should_construct_read_request_from_enum(MotorReadRegisters[] registers, string expected)
        {
            var result = ConstructPayloadFromReadMotorRegister(registers);
            Check.That(result).Equals(expected);
        }
        
        [Test]
        public void write_motor_setpoint_when_serial_port_fails_at_writing_request_with_retries()
        {
            var comPortMock = new Mock<IBhSerialPort>();
            comPortMock.Setup(m => m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Throws(new TimeoutException());


            var timeoutSettings = new TimeoutSettings(){Retries = 2, ReadWriteDelay = 0, WriteReadDelay = 0, Timeout = 50};
            var result = SimpaApi2.WriteMotor(MotorIds.M1, 42f, comPortMock.Object,timeoutSettings);
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error).IsInstanceOf<SimpaApiError>();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(0, 3));
            comPortMock.Verify(m=>m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            comPortMock.Verify(m=>m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(0));
        }
        
        [Test]
        public void write_motor_setpoint_when_serial_port_fails_at_reading_response_with_retries()
        {
            var comPortMock = new Mock<IBhSerialPort>();
            comPortMock.Setup(m => m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Throws(new Exception("boom"));

            var timeoutSettings = new TimeoutSettings(){Retries = 2, ReadWriteDelay = 0, WriteReadDelay = 0, Timeout = 50};
            var result = SimpaApi2.WriteMotor(MotorIds.M1, 42f,comPortMock.Object,timeoutSettings);
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error).IsInstanceOf<SimpaApiError>();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(0, 3));
            comPortMock.Verify(m=>m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
            comPortMock.Verify(m=>m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(3));
        }
        
        [Test]
        public void write_motor_setpoint_fails_then_succeeds()
        {
            
            var cnt = 0;
            var responseBytes = Encoding.ASCII.GetBytes($"{b(ACK)}\x42{b(XON)}");

            var comPortMock = new Mock<IBhSerialPort>();
            comPortMock.Setup(m => m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<byte[],int, int>((b, o, l) =>
                {
                    cnt += 1;
                    if (cnt % 2 != 0) throw new Exception("boom");
                    responseBytes.CopyTo(b,o);
                    return responseBytes.Length;
                });


            var timeoutSettings = new TimeoutSettings(){Retries = 2, ReadWriteDelay = 0, WriteReadDelay = 0, Timeout = 50};
            var result = SimpaApi2.WriteMotor(MotorIds.M1, 42f, comPortMock.Object,timeoutSettings);
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(1, 1));
        }

        [Test]
        public void read_motors_state_when_serial_port_fails_at_read_response()
        {
            var comPortMock = new Mock<IBhSerialPort>();
            comPortMock.Setup(m => m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Throws(new Exception("boom"));


            var timeoutSettings = new TimeoutSettings(){Retries = 2, ReadWriteDelay = 0, WriteReadDelay = 0, Timeout = 50};
            var result = SimpaApi2.ReadMotor(MotorIds.M1, comPortMock.Object,timeoutSettings);
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error).IsInstanceOf<SimpaApiError>();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(0, 1));
            comPortMock.Verify(m=>m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(1));
            comPortMock.Verify(m=>m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(1));
        }
        
        [Test]
        public void read_motors_state_when_serial_port_fails_at_write_request()
        {
            var comPortMock = new Mock<IBhSerialPort>();
            comPortMock.Setup(m => m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Throws(new Exception("boom"));


            var timeoutSettings = new TimeoutSettings(){Retries = 2, ReadWriteDelay = 0, WriteReadDelay = 0, Timeout = 50};
            var result = SimpaApi2.ReadMotor(MotorIds.M1,comPortMock.Object,timeoutSettings);
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error).IsInstanceOf<SimpaApiError>();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(0, 1));
            
            comPortMock.Verify(m=>m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(1));
            comPortMock.Verify(m=>m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(0));

        }
        
        [Test]
        public void read_serial_port_success_case()
        {
            var comPortMock = new Mock<IBhSerialPort>();
            var bytes = Encoding.ASCII.GetBytes($"{b(ACK)}\x42{b(STX)}01001BSP=+120{Checksum("01BSP=+120")}{b(ETX)}{b(XON)}");
            comPortMock.Setup(m => m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns<byte[],int,int>(
                (b, offset, l) =>
                {
                    bytes.CopyTo(b,offset);
                    return bytes.Length;
                });

            var timeoutSettings = new TimeoutSettings(){Retries = 2, ReadWriteDelay = 0, WriteReadDelay = 0, Timeout = 50};
            var result = SimpaApi2.ReadMotor(MotorIds.M1, comPortMock.Object,timeoutSettings);
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Both).IsEqualTo(new CommunicationDetails(1, 0));
            comPortMock.Verify(m=>m.Write(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(1));
            comPortMock.Verify(m=>m.Read(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()), Times.Exactly(1));
        }
    }
}