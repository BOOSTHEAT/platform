using System.Collections.Generic;
using ImpliciX.Api.TcpModbus.Infrastructure;
using Moq;
using NFluent;
using NModbus;
using NModbus.Data;
using NModbus.Message;
using NUnit.Framework;

namespace ImpliciX.Api.TCPModbus.Tests.Infrastructure;

[TestFixture]
public class ModbusSlaveSpyTests
{
  private readonly Mock<IModbusSlave> _mock = new();
  private IModbusSlave _slave;

  [SetUp]
  public void Setup()
  {
    _slave = _mock.Object;
  }

  [Test]
  public void The_parameters_of_a_WriteSingleRegisterRequestResponse_are_observed()
  {
    byte slaveAddress = 1;
    ushort startAddress = 5;
    ushort registerValue = 13;
    var decodedMessageModbus = new List<(ushort, ushort[])>();
    var sut = new ModbusSlaveSpy(_slave);
    sut.DataStoreWrittenTo += (sender, args) => { decodedMessageModbus.Add((args.startAddress, args.DataStore)); };
    var message = new WriteSingleRegisterRequestResponse(slaveAddress, startAddress, registerValue);
    _mock.Setup(slave => slave.ApplyRequest(message)).Returns(message);


    var messageResult = sut.ApplyRequest(message);

    Assert.AreEqual(decodedMessageModbus,
      new List<(ushort, ushort[])> { (startAddress, new ushort[] { registerValue }) });
  }

  [Test]
  public void The_parameters_of_a_WriteMultipleRegistersRequest_are_observed()
  {
    byte slaveAddress = 1;
    ushort startAddress = 5;
    var valuesTest = new ushort[] { 5, 17, 852 };
    var registerValue = new RegisterCollection(valuesTest);
    var decodedMessageModbus = new List<(ushort, ushort[])>();
    var sut = new ModbusSlaveSpy(_slave);
    sut.DataStoreWrittenTo += (sender, args) => { decodedMessageModbus.Add((args.startAddress, args.DataStore)); };
    var message = new WriteMultipleRegistersRequest(slaveAddress, startAddress, registerValue);
    _mock.Setup(slave => slave.ApplyRequest(message)).Returns(message);


    var messageResult = sut.ApplyRequest(message);

    Assert.AreEqual(decodedMessageModbus, new List<(ushort, ushort[])> { (startAddress, valuesTest) });
  }

  [Test]
  public void ApplyRequestdoesn_t_c_rash_even_if_nobody_is_listening_WriteSingleRegisterRequestResponse()
  {
    byte slaveAddress = 1;
    ushort startAddress = 5;
    ushort registerValue = 13;

    var sut = new ModbusSlaveSpy(_slave);
    var message = new WriteSingleRegisterRequestResponse(slaveAddress, startAddress, registerValue);
    _mock.Setup(slave => slave.ApplyRequest(message)).Returns(message);

    var messageResult = sut.ApplyRequest(message);
  }

  [Test]
  public void ApplyRequestdoesn_t_c_rash_even_if_nobody_is_listening_WriteMultipleRegistersRequest()
  {
    byte slaveAddress = 1;
    ushort startAddress = 5;


    var sut = new ModbusSlaveSpy(_slave);
    var message = new WriteMultipleRegistersRequest(slaveAddress, startAddress, new RegisterCollection());
    _mock.Setup(slave => slave.ApplyRequest(message)).Returns(message);


    var messageResult = sut.ApplyRequest(message);
  }

  [Test]
  public void When_ApplyRequest_is_called_it_returns_the_same_message()
  {
    var sut = new ModbusSlaveSpy(_slave);
    var mockMessage = new Mock<IModbusMessage>();
    var message = mockMessage.Object;
    _mock.Setup(slave => slave.ApplyRequest(message)).Returns(message);


    var messageResult = sut.ApplyRequest(message);


    Check.That(messageResult).IsEqualTo(message);
  }
}