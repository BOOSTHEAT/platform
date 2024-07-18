using System;
using System.Collections.Generic;
using ImpliciX.Driver.Common.Buffer;
using ImpliciX.Driver.Common.CommandAggregator;
using ImpliciX.Language.Model;

namespace ImpliciX.Motors.Controllers.Buffer
{
    public static class MotorCommandRequestFactory
    {
        public static ICommandRequestedBuffer Create()
        {
            var aggregatorMap = new Dictionary<Type, ICommandRequestedBuffer>
            {
                {typeof(RotationalSpeed), new CommandRequestedAggregatorBuffer(YoungestCommandAggregator.Combine)}
            };

            return new CommandRequestedBuffer(aggregatorMap);
        }
    }
}