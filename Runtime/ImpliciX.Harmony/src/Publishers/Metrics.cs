using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Messages.Formatter;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Harmony.Publishers
{
    public class Metrics : Publisher
    {
        public Metrics(Queue<IHarmonyMessage> elementsQueue) : base(elementsQueue)
        {
        }

        public override void Handles(PropertiesChanged propertiesChanged)
        {
            var analytics =
                propertiesChanged.ModelValues
                    .Where(c => c.ModelValue() is MetricValue)
                    .Select(c => (c.Urn, (MetricValue) c.ModelValue()))
                    .Select(c => new Analytics(
                        GetDateTime(propertiesChanged).Format(),
                        c.Urn,
                        c.Item2.Value,
                        c.Item2.SamplingStartDate.Format(),
                        c.Item2.SamplingEndDate.Format()))
                    .ToArray();


            if (analytics.Length > 0)
            {
                var message = new AnalyticsMessage(analytics.ToArray());
                ElementsQueue.Enqueue(message);
            }
        }
    }

    public readonly struct Analytics
    {
        public Analytics(string publicationDateTime, string urn, float value,
            string sampleStartTime, string sampleEndTime)
        {
            PublicationDateTime = publicationDateTime;
            Urn = urn;
            Value = value;
            SampleStartTime = sampleStartTime;
            SampleEndTime = sampleEndTime;
        }

        public ushort Version => 1;
        public string PublicationDateTime { get; }
        public string SampleStartTime { get; }
        public string SampleEndTime { get; }
        public string Urn { get; }
        public float Value { get; }
    }

    public class AnalyticsMessage : IHarmonyMessage
    {
        private readonly Analytics[] _analytics;

        public AnalyticsMessage(Analytics[] analytics)
        {
            _analytics = analytics;
        }

        public string Format(IPublishingContext context)
        {
            var sb = new StringBuilder();
            foreach (var analytics in _analytics)
            {
                var json = new AnalyticsJson(context.SerialNumber, analytics);
                sb.Append(json.Format()).Append(Environment.NewLine);
            }
            return sb.ToString();
        }

        public string GetMessageType() => "Analytics";
    }

    public readonly struct AnalyticsJson
    {
        public AnalyticsJson(string serialNumber, Analytics analytics)
        {
            SerialNumber = serialNumber;
            PublicationDateTime = analytics.PublicationDateTime;
            Urn = analytics.Urn;
            Value = analytics.Value;
            SampleStartTime = analytics.SampleStartTime;
            SampleEndTime = analytics.SampleEndTime;
        }

        public ushort Version => 1;
        public string SerialNumber { get; }
        public string PublicationDateTime { get; }
        public string SampleStartTime { get; }
        public string SampleEndTime { get; }
        public string Urn { get; }
        public float Value { get; }
    }
}