using System;
using System.Collections.Generic;
using ImpliciX.Driver.Common.Buffer;
using ImpliciX.Driver.Common.CommandAggregator;
using ImpliciX.Language.Model;

namespace ImpliciX.RTUModbus.Controllers.Helpers
{
    public static class ModbusCommandRequestFactory
    {
        public static ICommandRequestedBuffer Create()
        {
            var aggregatorMap = new Dictionary<Type, ICommandRequestedBuffer>
            {
                {typeof(Percentage), new CommandRequestedAggregatorBuffer(YoungestCommandAggregator.Combine)},
                {typeof(Temperature), new CommandRequestedAggregatorBuffer(YoungestCommandAggregator.Combine)}
            };

            return new CommandRequestedBuffer(aggregatorMap);
        }
    }
}