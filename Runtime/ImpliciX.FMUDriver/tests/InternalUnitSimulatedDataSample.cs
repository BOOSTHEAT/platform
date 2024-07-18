using System;
using System.Collections.Generic;
using ImpliciX.Language.Model;

namespace ImpliciX.FmuDriver.Tests
{
    public class InternalUnitSimulatedDataSample
    {
         public static IEnumerable<IDataModelValue> DecodedRegistersNominalCase(TimeSpan currentTime)
        {
            return new IDataModelValue[]
            {
                Property<Temperature>.Create(fake.service_dhw_bottom_temperature.measure, Temperature.Create(0),currentTime),
                Property<MeasureStatus>.Create(fake.service_dhw_bottom_temperature.status, MeasureStatus.Success, currentTime),
                Property<RotationalSpeed>.Create(fake.production_auxiliary_burner_fan_speed.measure, RotationalSpeed.FromFloat(0).Value,currentTime)
            };
        }
    }
}