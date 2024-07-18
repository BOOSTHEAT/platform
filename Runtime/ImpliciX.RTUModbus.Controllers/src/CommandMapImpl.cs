using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;

namespace ImpliciX.RTUModbus.Controllers;

public class CommandMapImpl : ICommandMap
{
  private readonly Dictionary<Urn, CommandActuator> _modbusCommandFactory;
  private readonly Dictionary<Urn, IMeasure> _measureMap;
        
  public CommandMapImpl()
  {
    _modbusCommandFactory = new Dictionary<Urn, CommandActuator>();
    _measureMap = new Dictionary<Urn, IMeasure>();
  }

  public ICommandMap Add<T>(CommandNode<T> commandNode, CommandActuator actuatorFunc)
  {
    _modbusCommandFactory.Add(commandNode.command, actuatorFunc);
    var measure = new LateMeasure<T>(commandNode.measure, commandNode.status);
    _measureMap.Add(commandNode.command,measure);
    return this;
  }

  public CommandActuator ModbusCommandFactory(Urn urn) => _modbusCommandFactory.GetValueOrDefault(urn);
        
  public bool ContainsKey(Urn commandUrn) => _modbusCommandFactory.ContainsKey(commandUrn);

  public IMeasure Measure(Urn commandUrn)
  {
    Debug.PreCondition(() => _measureMap.ContainsKey(commandUrn), () => $"CommandMap does not contains urn {commandUrn}");
    return _measureMap[commandUrn];
  }
}