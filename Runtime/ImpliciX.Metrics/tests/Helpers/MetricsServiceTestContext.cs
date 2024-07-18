using System;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Collections;
using ImpliciX.TestsCommon;
using NFluent;

namespace ImpliciX.Metrics.Tests.Helpers;

internal class MetricsServiceTestContext
{
    private readonly MetricsService _sut;
    private Property<MetricValue>[] _lastPropertiesReceived;

    public MetricsServiceTestContext(MetricsService sut)
    {
        _sut = sut;
    }

    public Property<MetricValue>[] AdvanceTimeTo(TimeSpan at)
    {
         var events = _sut.HandleSystemTicked(SystemTicked.Create(TimeSpan.Zero, 1000, (uint) at.TotalSeconds));

        _lastPropertiesReceived = events
            .OfType<PropertiesChanged>()
            .SelectMany(o => o.ModelValues.OfType<Property<MetricValue>>())
            .ToArray();

        return _lastPropertiesReceived;
        
       
    }

    public Property<MetricValue>[] AdvanceTimeTo(int atInMinutes)
    {
        return AdvanceTimeTo(TimeSpan.FromMinutes(atInMinutes));
    }

    public void ChangeFloatTo(string urn, int atInMinutes, float newValue)
        => ChangeFloatTo(urn, TimeSpan.FromMinutes(atInMinutes), newValue);

    public void ChangeFloatTo(string urn, TimeSpan at, float newValue)
        => _sut.HandlePropertiesChanged(PropertiesChanged.Create(new[] {CreateProperty(urn, newValue, at)}, at));

    public void ChangeFakeIndexTo(float val, int atInMinute)
        => _sut.HandlePropertiesChanged(PropertiesChanged.Create(fake_model.fake_index, Flow.FromFloat(val).Value, TimeSpan.FromMinutes(atInMinute)));

    public void ChangeTemperature(int atInMinutes, float newValue)
        => _sut.HandlePropertiesChanged(PropertiesChanged.Create(new[] {CreateTemperatureProperty(newValue, atInMinutes)}, TimeSpan.FromMinutes(atInMinutes)));

    public void ChangeTemperature(TimeSpan at, float newValue)
        => _sut.HandlePropertiesChanged(PropertiesChanged.Create(new[] {CreateTemperatureProperty(newValue, at)}, at));

    public void ChangeStateTo(fake_model.PublicState state, int atInMinute)
        => _sut.HandlePropertiesChanged(CreateStateChanged(state, atInMinute));

    public void CheckLastPropertiesReceived(params Property<MetricValue>[] propertiesContentExpected)
    {
        if (propertiesContentExpected.IsEmpty())
        {
            Check.That(_lastPropertiesReceived).IsEmpty();
            return;
        }

        Check.That(_lastPropertiesReceived).ContainsExactly(propertiesContentExpected);
    }

    private static PropertiesChanged CreateStateChanged(fake_model.PublicState state, int atInMinute)
        => PropertiesChanged.Create(
            new[]
            {
                Property<fake_model.PublicState>.Create(fake_model.public_state, state, TimeSpan.FromMinutes(atInMinute))
            }, TimeSpan.FromMinutes(atInMinute));

    private static IDataModelValue CreateTemperatureProperty(float value, int atInMinutes)
        => CreateTemperatureProperty(value, TimeSpan.FromMinutes(atInMinutes));

    private static IDataModelValue CreateTemperatureProperty(float value, TimeSpan at)
        => Property<Temperature>.Create(fake_model.temperature.measure, Temperature.Create(value), at);

    private static IDataModelValue CreateProperty(string urn, float value, TimeSpan at)
        => Property<FloatValue>.Create(PropertyUrn<FloatValue>.Build(urn), new FloatValue(value), at);
}