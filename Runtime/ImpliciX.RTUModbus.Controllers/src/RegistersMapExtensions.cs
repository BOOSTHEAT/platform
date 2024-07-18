using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Driver;
using ImpliciX.Language.Modbus;
using ImpliciX.Language.Model;

namespace ImpliciX.RTUModbus.Controllers
{
  public static class RegistersMapExtensions
  {
    public static Result<IEnumerable<IDataModelValue>> Eval(this IRegistersMap map, ModbusRegisters readResult,
      TimeSpan currentTime, IDriverStateKeeper driverStateKeeper)
    {
      var measuresResult = map.Conversions.Aggregate(
        new List<Result<IMeasure>>(),
        (acc, conversionDefinition) =>
        {
          acc.Add(conversionDefinition.Eval(readResult, currentTime, driverStateKeeper));
          return acc;
        });
      return Traverse(measuresResult)
        .Match(
          whenError: Result<IEnumerable<IDataModelValue>>.Create,
          whenSuccess: measures =>
            Result<IEnumerable<IDataModelValue>>.Create(measures.SelectMany(m => m.ModelValues())));
    }
    
    public static Result<IEnumerable<T>> Traverse<T>(IEnumerable<Result<T>> @this)
    {
      var items = @this.ToArray();
      var errors = items.Where(r => r.IsError).Select(r => r.Error).ToList();
      return errors.Any()
        ? errors.Aggregate((e1, e2) => e1.Merge(e2))
        : Result<IEnumerable<T>>.Create(items.Where(r => r.IsSuccess).Select(r => r.Value).ToList());
    }

    public static Result<IMeasure> Eval(this IConversionDefinition conversion, ModbusRegisters modbusRegisters,
      TimeSpan currentTime,
      IDriverStateKeeper driverStateKeeper)
    {
      var readRegistersResult = modbusRegisters.Extract(conversion.Slices);
      if (readRegistersResult.IsError)
        return new FailedMeasure(conversion.StatusUrn, currentTime);
      var decoded = conversion.Decode(readRegistersResult.Value, currentTime, driverStateKeeper);
      if (decoded == null)
      {
        Log.Error("Unexpected null value when decoding {MeasureUrn}", conversion.MeasureUrn);
        return new FailedMeasure(conversion.StatusUrn, currentTime);
      }

      return decoded;
    }
  }
}