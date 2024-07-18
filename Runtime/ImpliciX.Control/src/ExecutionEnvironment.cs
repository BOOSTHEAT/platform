using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Control
{
    public interface IExecutionEnvironment
    {
        PropertiesChanged Changed(PropertiesChanged propertiesChanged);
        Result<IDataModelValue> GetProperty(Urn urn);
        void SetTimeoutRequestInstance(NotifyOnTimeoutRequested request);
        bool CheckTimeoutRequestExists(TimeoutOccured timeout);
    }

    public class ExecutionEnvironment : IExecutionEnvironment
    {
        private readonly ConcurrentDictionary<Urn, IDataModelValue> _properties = new ConcurrentDictionary<Urn, IDataModelValue>();
        private readonly ConcurrentDictionary<Urn, Guid> _timeoutRequests = new ConcurrentDictionary<Urn, Guid>();

        public PropertiesChanged Changed(PropertiesChanged propertiesChanged)
        {
            var changedProps = new List<IDataModelValue>();
            propertiesChanged
                .ModelValues
                .ToList()
                .ForEach(prop =>
                {
                    _properties[prop.Urn] = prop;
                    changedProps.Add(prop);
                });

            return propertiesChanged;
        }

        public Result<IDataModelValue> GetProperty(Urn urn)
        {
            if (ConstantProperties.ContainsKey(urn)) return Result<IDataModelValue>.Create(ConstantProperties[urn]);

            return _properties.TryGetValue(urn, out var property)
                ? Result<IDataModelValue>.Create(property)
                : PropertiesError.NotFound(urn);
        }

        public void SetTimeoutRequestInstance(NotifyOnTimeoutRequested request)
        {
            _timeoutRequests.AddOrUpdate(request.TimerUrn, _ => request.EventId, (_, __) => request.EventId);
        }

        public bool CheckTimeoutRequestExists(TimeoutOccured timeout) =>
            _timeoutRequests.TryGetValue(timeout.TimerUrn, out var guid) && guid.Equals(timeout.RequestId);

        private static readonly Dictionary<Urn, IDataModelValue> ConstantProperties =
            new Dictionary<Urn, IDataModelValue>()
            {
                [constant.parameters.temperature.zero] =
                    Property<Temperature>.Create(constant.parameters.temperature.zero, Temperature.Create(0f),
                        TimeSpan.Zero),
                [constant.parameters.percentage.zero] = Property<Percentage>.Create(constant.parameters.percentage.zero,
                    Percentage.FromFloat(0f).Value, TimeSpan.Zero),
                [constant.parameters.percentage.one] = Property<Percentage>.Create(constant.parameters.percentage.one,
                    Percentage.FromFloat(0.01f).Value, TimeSpan.Zero),
                [constant.parameters.percentage.hundred] =
                    Property<Percentage>.Create(constant.parameters.percentage.hundred, Percentage.FromFloat(1f).Value,
                        TimeSpan.Zero),
                [constant.parameters.displacement_queue.zero] = Property<DisplacementQueue>.Create(
                    constant.parameters.displacement_queue.zero, DisplacementQueue.FromShort(0).GetValueOrDefault(),
                    TimeSpan.Zero),
                [constant.parameters.displacement_queue.one] = Property<DisplacementQueue>.Create(
                    constant.parameters.displacement_queue.one, DisplacementQueue.FromShort(1).GetValueOrDefault(),
                    TimeSpan.Zero),
                [constant.parameters.power.zero] = Property<Power>.Create(constant.parameters.power.zero,
                    Power.FromFloat(0f).Value, TimeSpan.Zero)
            };
    }

    public class PropertiesError : Error
    {
        public static PropertiesError NotFound(Urn urn) => new PropertiesError($"No property found for {urn}");

        private PropertiesError(string message) : base(nameof(PropertiesError), message)
        {
        }
    }
}