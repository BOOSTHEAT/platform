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
    public class Alarms : Publisher
    {
        public Alarms(Func<PropertyUrn<AlarmState>, Option<string>> alarmCodeFromAlarmStateUrn,
            Queue<IHarmonyMessage> elementsQueue)
            : base(elementsQueue)
        {
            _alarmCodeFromAlarmStateUrn = alarmCodeFromAlarmStateUrn;
        }

        public override void Handles(PropertiesChanged propertiesChanged)
        {
            var alarmStates = (from modelValue in propertiesChanged.ModelValues
                where modelValue is Property<AlarmState>
                select (Property<AlarmState>) modelValue).ToArray();
            if (alarmStates.Length == 0)
                return;
            var alerts =
                from alert in
                    from alarm in alarmStates
                    select Alarm.Create(alarm, _alarmCodeFromAlarmStateUrn)
                        .Tap(() => Log.Error($"Can't create Alert for {alarm.Urn}"), _ => { })
                where alert.IsSome
                select alert.GetValue();
            var message = new AlarmsMessage(GetDateTime(propertiesChanged).Format(), alerts.ToArray());
            ElementsQueue.Enqueue(message);
        }

        private readonly Func<PropertyUrn<AlarmState>, Option<string>> _alarmCodeFromAlarmStateUrn;
    }

    public struct Alarm
    {
        public static Option<Alarm> Create(IDataModelValue value,
            Func<PropertyUrn<AlarmState>, Option<string>> alarmNameFromUrn)
        {
            if (value.Urn is PropertyUrn<AlarmState> urn)
            {
                var state = (AlarmState) value.ModelValue();
                var process = state switch
                {
                    AlarmState.Active => "Abnormal",
                    AlarmState.Inactive => "Normal",
                    _ => throw new ArgumentOutOfRangeException()
                };

                return alarmNameFromUrn(urn).Select(
                    alarmName => new Alarm(alarmName, state.ToString(), process, value.At.Format()));
            }
            else
                return Option<Alarm>.None();
        }

        public string Code { get; set; }
        public string State { get; set; }
        public string Process { get; set; }
        public string Timestamp { get; set; }

        private Alarm(string alarmName, string state, string process, string timeStamp)
        {
            Code = alarmName;
            State = state;
            Process = process;
            Timestamp = timeStamp;
        }
    }

    public readonly struct AlarmsMessage : IHarmonyMessage
    {
        public AlarmsMessage(string dateTime, params Alarm[] alerts)
        {
            DateTime = dateTime;
            Alerts = alerts;
        }

        public string DateTime { get; }
        public Alarm[] Alerts { get; }

        public string Format(IPublishingContext context) => BasicFormatter.Format(new AlarmsMessageJson(context, this));

        public string GetMessageType() => "Alert";

        private bool Equals(AlarmsMessage other) =>
            DateTime == other.DateTime &&
            Alerts.SequenceEqual(other.Alerts);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AlarmsMessage) obj);
        }

        public override int GetHashCode() => HashCode.Combine(DateTime, Alerts);
    }

    public readonly struct AlarmsMessageJson
    {
        public AlarmsMessageJson(IPublishingContext context, AlarmsMessage alarm)
        {
            SerialNumber = context.SerialNumber;
            DateTime = alarm.DateTime;
            Alerts = alarm.Alerts;
        }

        public string SerialNumber { get; }
        public string DateTime { get; }
        public Alarm[] Alerts { get; }
    }
}