using System;
using ImpliciX.Language.Core;
using ImpliciX.Motors.Controllers.Domain;
using ImpliciX.Motors.Controllers.Settings;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.SerialApi2.SerialPort;

namespace ImpliciX.Motors.Controllers.Infrastructure
{
    public interface IMotorsInfrastructure
    {
        Result2<MotorResponse, CommunicationDetails> WriteSimpa(MotorIds motorId, float value);
        Result2<MotorResponse, CommunicationDetails> ReadSimpa(MotorIds motorId);
    }

    public class MotorsInfrastructure : IMotorsInfrastructure
    {
        private readonly MotorsDriverSettings _motorsDriverSettings;
        private BhSerialPort _serialPort;

        public static MotorsInfrastructure Create(MotorsDriverSettings motorsDriverSettings)
        {
            return new MotorsInfrastructure(motorsDriverSettings);
        }

        private MotorsInfrastructure(MotorsDriverSettings motorsDriverSettings)
        {
            _motorsDriverSettings = motorsDriverSettings;
            CreateCommunicationContext();
        }

        public Result2<MotorResponse, CommunicationDetails> ReadSimpa(MotorIds motorId)
        {
            return SimpaApi2.ReadMotor(motorId, _serialPort, _motorsDriverSettings.TimeoutSettings)
                .Tap(whenError: _=> ResetCommunicationContext(), whenSuccess:_=>{});
        }
        
        
        public Result2<MotorResponse, CommunicationDetails> WriteSimpa(MotorIds motorId, float value)
        {
            return SimpaApi2.WriteMotor(motorId, value, _serialPort, _motorsDriverSettings.TimeoutSettings)
                 .Tap(whenError: _=> ResetCommunicationContext(), whenSuccess:_=>{});
        }
        
        private void CreateCommunicationContext()
        {
            var serialPortSettings = _motorsDriverSettings.SerialPortSettings;
            _serialPort = new BhSerialPort(
                serialPortSettings.SerialPort,
                serialPortSettings.BaudRate,
                serialPortSettings.DataBits,
                Enum.Parse<Parity>(serialPortSettings.Parity, true),
                Enum.Parse<StopBits>(serialPortSettings.StopBits, true));
            _serialPort.Open();
        }

        private void ResetCommunicationContext()
        {
           _serialPort.Dispose(); 
           CreateCommunicationContext();
        }
    }
}