using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ImpliciX.Api.TcpModbus;
using ImpliciX.Api.TcpModbus.Infrastructure;
using NModbus;
using NUnit.Framework;

namespace ImpliciX.Api.TCPModbus.Tests.Infrastructure;

public class ModbusTcpSlaveAdapterTests
{
  [Test]
  public void WriteSingleRegisterWithNoOneListening()
  {
    RunModbusSequence(
      (sut,master) => master.WriteSingleRegister(1, 23, 66)
    );
  }
  
  [Test]
  public void WriteSingleRegister()
  {
    HandleModbusWrite(
      master =>
        master.WriteSingleRegister(1, 23, 66),
      (start, data) =>
      {
        Assert.That(start, Is.EqualTo(23));
        Assert.That(data, Is.EqualTo(new ushort[] {66}));
      }
    );
  }

  [Test]
  public void WriteMultipleRegistersWithNoOneListening()
  {
    RunModbusSequence(
      (sut,master) =>
        master.WriteMultipleRegisters(1, 23, new ushort[]{66,67})
    );
  }
  
  [Test]
  public void WriteMultipleRegisters()
  {
    HandleModbusWrite(
      master => master.WriteMultipleRegisters(1, 23, new ushort[]{66,67}),
      (start, data) =>
      {
        Assert.That(start, Is.EqualTo(23));
        Assert.That(data, Is.EqualTo(new ushort[] {66, 67}));
      }
    );
  }
  
  public void HandleModbusWrite(Action<IModbusMaster> act, Action<ushort,ushort[]> assert)
  {
    var calledBack = false;
    Exception thrown = null;
    RunModbusSequence((sut, master) =>
    {
      sut.OnHoldingRegisterUpdate += (sender, received) =>
      {
        calledBack = true;
        try
        {
          assert(received.startAddress, received.data);
        }
        catch (AssertionException e)
        {
          thrown = e;
        }
      };
      act(master);
    });
    Assert.True(calledBack, "No callback");
    if (thrown != null)
      throw thrown;
  }
  
  public void RunModbusSequence(Action<IModbusTcpSlaveAdapter,IModbusMaster> sequence)
  {
    var settings = new ModbusTcpSettings();
    using var sut = new ModbusTcpSlaveAdapter(settings);
    sut.Start();
    WaitUntilPortIsListening(settings.TCPPort);
    var factory = new ModbusFactory();
    using var master = factory.CreateMaster(new TcpClient("127.0.0.1", settings.TCPPort));
    sequence(sut,master);
    sut.Stop();
  }

  private static void WaitUntilPortIsListening(int port)
  {
    while(IPGlobalProperties.GetIPGlobalProperties()
          .GetActiveTcpListeners()
          .All(ep => ep.Port != port))
    {
    }
  }
}