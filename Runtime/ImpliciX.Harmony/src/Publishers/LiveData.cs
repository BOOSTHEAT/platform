using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Harmony.Messages;
using ImpliciX.Harmony.Messages.Formatter;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Harmony.Publishers
{
    public class LiveData : Publisher
    {
        public LiveData(HarmonyModuleDefinition.LiveDataModel liveDataModel, Queue<IHarmonyMessage> elementsQueue)
            : base(elementsQueue)
        {
            _isActive = false;
            _period = liveDataModel.Period;
            _activationUrn = liveDataModel.Presence;
            _selectedUrns = liveDataModel.Content.ToHashSet();
            _cache = new Dictionary<Urn, IDataModelValue>();
        }

        public override void Handles(SystemTicked ticked)
        {
            if (_isActive && ticked.IsNextDate(_period))
            {
                var message = new Message(GetDateTime(ticked), _cache.Values.ToArray());
                ElementsQueue.Enqueue(message);
                _cache.Clear();
            }
        }

        public override void Handles(PropertiesChanged propertiesChanged)
        {
            foreach (var mv in propertiesChanged.ModelValues)
            {
                if (mv.Urn == _activationUrn && ((Presence) mv.ModelValue()) == Presence.Enabled)
                {
                    Log.Information($"Harmony live data {mv.ModelValue()} with interval {_period}");
                    _isActive = true;
                    continue;
                }
                if (!_selectedUrns.Contains(mv.Urn))
                    continue;
                _cache[mv.Urn] = mv;
            }
        }

        private readonly Dictionary<Urn, IDataModelValue> _cache;
        private readonly HashSet<Urn> _selectedUrns;
        private readonly TimeSpan _period;
        private bool _isActive;
        private readonly PropertyUrn<Presence> _activationUrn;


        private class Message : IHarmonyMessage
        {
            private readonly DateTime _dateTime;
            private readonly IEnumerable<IDataModelValue> _modelValues;

            public Message(DateTime dateTime, IEnumerable<IDataModelValue> modelValues)
            {
                _dateTime = dateTime;
                _modelValues = modelValues;
            }

            public string Format(IPublishingContext context) =>
                new LiveDataJson(
                    context,
                    _dateTime.Format(),
                    CreateData(_modelValues)
                ).Format();

            IDictionary<string, float> CreateData(IEnumerable<IDataModelValue> modelValues)
            {
                var obj = new Dictionary<string, float>();
                foreach (var mv in modelValues)
                {
                    var result = mv.ToFloat();
                    result.Tap(
                        e => Log.Warning(e.Message),
                        v => obj.Add(mv.Urn, v)
                    );
                }

                return obj;
            }

            public string GetMessageType() => "LiveData";
        }


        public readonly struct LiveDataJson
        {
            public LiveDataJson(IPublishingContext context, string dateTime, IDictionary<string, float> data)
            {
                SerialNumber = context.SerialNumber;
                DateTime = dateTime;
                Data = data;
            }

            public string SerialNumber { get; }
            public string DateTime { get; }
            public IDictionary<string, float> Data { get; }
        }
    }
}