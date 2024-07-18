using ImpliciX.RuntimeFoundations.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Api.TcpModbus;

public class ModbusPublisher
{
  private readonly List<(ushort startAddress, TimeSpan time, ushort[] dataStore)> _eventsData;
  private readonly Func<TimeSpan> _now;

  public ModbusPublisher(IModbusTcpSlaveAdapter slaveAdapter, Func<TimeSpan> clock)
  {
    _eventsData = new List<(ushort startAddress, TimeSpan time, ushort[] dataStore)>();
    _now = clock;
    slaveAdapter.OnHoldingRegisterUpdate += (sender, args) =>
    {
      _eventsData.Add((args.startAddress, _now(), args.data));
    };
  }

  public DomainEvent[] PullDomainEvents(ModbusMapping modbusMapping)
  {
    var modelValues = _eventsData
      .SelectMany(eventData =>
        modbusMapping.RegistersToDataModelValues(eventData.startAddress, eventData.dataStore, eventData.time));
    var result = CreateDomainEventsFromProperties(modelValues).ToArray();
    _eventsData.Clear();
    return result;
  }
  
  private IEnumerable<DomainEvent> CreateDomainEventsFromProperties(IEnumerable<object> instances)
  {
    var standardProperties = new Dictionary<Urn,IDataModelValue>();
    var settingProperties = new Dictionary<Urn,IDataModelValue>();
    foreach (var instance in instances)
    {
      if (instance is IModelCommand command)
      {
        yield return CommandRequested.Create(command, _now());
        continue;
      }
      var property = (IDataModelValue)instance;
      var propertySet = property.Urn is ISettingUrn ? settingProperties : standardProperties;
      propertySet[property.Urn] = property;
    }
    var ts = _now();
    if (standardProperties.Any())
      yield return PropertiesChanged.Create(standardProperties.Values, ts);
    if (settingProperties.Any())
      yield return PersistentChangeRequest.Create(settingProperties.Values, ts);
  }
}