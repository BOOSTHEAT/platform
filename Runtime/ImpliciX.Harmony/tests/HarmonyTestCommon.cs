using System;
using System.Linq;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Harmony.Tests
{
    public static class HarmonyTestCommon
    {
        public static Property<AlarmState> CreateAlarmProperty(DateTime dateTime, AlarmState alarmState,
            AlarmNode alarmNode) =>
            Property<AlarmState>.Create(alarmNode.state, alarmState, TimeSpan.FromTicks(dateTime.Ticks));

        public static PropertiesChanged CreateAlarmPropertyChanged(DateTime dateTime, AlarmState alarmState,
            AlarmNode alarmNode) =>
            PropertiesChanged.Create(alarmNode.state, alarmState, TimeSpan.FromTicks(dateTime.Ticks));

        public static Property<T> CreateAnalyticsProperty<T>(
            PersistentCounterUrn<T> urn,
            T enumCounter, DateTime dateTime) =>
            Property<T>.Create(urn, enumCounter, TimeSpan.FromTicks(dateTime.Ticks));
    }

    public class ContextStub : IPublishingContext
    {
        public ContextStub(string serialNumber)
        {
            SerialNumber = serialNumber;
        }

        public string SerialNumber { get; }
    }

    public class test_model : RootModelNode
    {
        static test_model()
        {
            dummy = PropertyUrn<Literal>.Build(nameof(dummy), nameof(test_counters_a));
            C998 = new AlarmNode(nameof(C998), new test_model("test_model"));
            C999 = new AlarmNode(nameof(C999), new test_model("test_model"));
            test_counters_a =
                MetricUrn.Build(nameof(test_model), nameof(test_counters_a));
            test_counters_b =
                MetricUrn.Build(nameof(test_model), nameof(test_counters_b));
            test_sample_accumulator =
                MetricUrn.Build(nameof(test_model), nameof(test_sample_accumulator));
            test_gauge =
                MetricUrn.Build(nameof(test_model), nameof(test_gauge));
            has_live_data = PropertyUrn<Presence>.Build("root", nameof(has_live_data));
            temperature = PropertyUrn<Temperature>.Build("root", nameof(temperature));
            pressure = PropertyUrn<Pressure>.Build("root", nameof(pressure));
            energy = PropertyUrn<Energy>.Build("root", nameof(energy));
            burner_status = PropertyUrn<GasBurnerStatus>.Build("root", nameof(burner_status));
            additionalID1 = PropertyUrn<Literal>.Build("root", nameof(additionalID1));
            additionalID2 = PropertyUrn<Literal>.Build("root", nameof(additionalID2));
        }

        public static PropertyUrn<Literal> dummy { get; }
        public static AlarmNode C998 { get; }
        public static AlarmNode C999 { get; }
        public static MetricUrn test_counters_a { get; }
        public static MetricUrn test_counters_b { get; }
        public static MetricUrn test_sample_accumulator { get; }
        public static MetricUrn test_gauge { get; }
        public static PropertyUrn<Presence> has_live_data { get; }
        public static PropertyUrn<Temperature> temperature { get; }
        public static PropertyUrn<Pressure> pressure { get; }
        public static PropertyUrn<Energy> energy { get; }
        public static PropertyUrn<GasBurnerStatus> burner_status { get; }
        public static PropertyUrn<Literal> additionalID1 { get; }
        public static PropertyUrn<Literal> additionalID2 { get; }

        public test_model(string urnToken) : base(urnToken)
        {
        }
    }

    public static class DomainEventTestExtension
    {
        public static Type[] GetTypes(this DomainEvent[] domainEvents)
        {
            return domainEvents.Select(@event => @event.GetType()).ToArray();
        }
    }
}