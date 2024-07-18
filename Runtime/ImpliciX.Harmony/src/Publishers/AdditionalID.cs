using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Messages.Formatter;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Harmony.Publishers
{
    public class AdditionalID : Publisher
    {
        public AdditionalID(
            Dictionary<string, PropertyUrn<Literal>> definition,
            Queue<IHarmonyMessage> elementsQueue)
            : base(elementsQueue)
        {
            _selectedIDs = definition.ToDictionary(kv => (Urn) kv.Value, kv => kv.Key);
        }

        private readonly Dictionary<Urn, string> _selectedIDs;

        public override void Handles(PropertiesChanged propertiesChanged)
        {
            var referencedIDs = propertiesChanged.ModelValues
                .Select(mv
                    => _selectedIDs.TryGetValue(mv.Urn, out string idName)
                        ? (idName, (Literal) mv.ModelValue())
                        : ((string idName, Literal)?) null
                ).Where(x => x.HasValue)
                .Select(x => x.Value).ToArray();
            if (referencedIDs.Length == 0)
                return;
            Log.Information($"Harmony additional IDs: {string.Join(", ", referencedIDs.Select(i => $"{i.idName}={i.Item2}"))}");
            var message = new Message(GetDateTime(propertiesChanged), referencedIDs);
            ElementsQueue.Enqueue(message);
        }

        private class Message : IHarmonyMessage
        {
            private readonly DateTime _dateTime;
            private readonly IEnumerable<(string key, Literal value)> _ids;

            public Message(DateTime dateTime, IEnumerable<(string key, Literal value)> ids)
            {
                _dateTime = dateTime;
                _ids = ids;
            }

            public string Format(IPublishingContext context) =>
                new JsonMessage(
                    context,
                    _dateTime.Format(),
                    _ids.ToDictionary(x => x.key, x => x.value.ToString())
                ).Format();

            public string GetMessageType() => "AdditionalID";
        }

        public readonly struct JsonMessage
        {
            public JsonMessage(IPublishingContext context, string dateTime, IDictionary<string, string> data)
            {
                SerialNumber = context.SerialNumber;
                DateTime = dateTime;
                Data = data;
            }

            public string SerialNumber { get; }
            public string DateTime { get; }
            public IDictionary<string, string> Data { get; }
        }
    }
}