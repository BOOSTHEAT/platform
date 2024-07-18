using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Language;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.SharedKernel.Clock;

namespace ImpliciX.Watchdog
{
    public class Controller
    {
        private readonly Dictionary<Urn, string> _modulesByAlarmUrn;
        private readonly IClock _clock;
        private readonly Action<CommandUrn<NoArg>> _restartAppFunc;
        private readonly int _panicDelayBeforeRestart;
        private readonly Dictionary<(Urn, string), IDisposable> _timers = new Dictionary<(Urn, string), IDisposable>();
        private readonly CommandUrn<NoArg> _restartBoilerAppUrn;

        public Controller(Settings settings, WatchdogModuleDefinition watchdogs, IClock clock, Action<CommandUrn<NoArg>> restartAppFunc)
        {
            _clock = clock;
            _restartAppFunc = restartAppFunc;
            _restartBoilerAppUrn = watchdogs.Restart;
            _modulesByAlarmUrn = watchdogs.InputOutputPanic.ToDictionary(
                kv => kv.Key,
                kv => settings.Modules[kv.Value]);
            _panicDelayBeforeRestart = settings.PanicDelayBeforeRestart;
        }

        public DomainEvent[] HandlePropertiesChanged(PropertiesChanged propertiesChanged)
        {
            var alarms = (
                from modelValue in propertiesChanged.ModelValues
                let moduleId = _modulesByAlarmUrn.TryGetValue(modelValue.Urn, out var mid) ? mid : null
                where moduleId != null
                select new {alarmUrn = modelValue.Urn, alarmState = (AlarmState) modelValue.ModelValue(), moduleId}).ToArray();
            foreach (var alarm in alarms) ProcessTimer(alarm.alarmUrn, alarm.alarmState, alarm.moduleId);
            return alarms.Where(c => c.alarmState == AlarmState.Active)
                .GroupBy(a => a.moduleId)
                .Select(module => ModulePanic.Create(module.Key, _clock.Now()))
                .Cast<DomainEvent>().ToArray();
        }

        private void ProcessTimer(Urn alarmUrn, AlarmState alarmState, string moduleId)
        {
            switch (alarmState)
            {
                case AlarmState.Active:
                    Log.Warning($"PANIC in {moduleId}");
                    StartTimer(moduleId, alarmUrn);
                    break;
                case AlarmState.Inactive:
                    ResetTimer(moduleId, alarmUrn);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void StartTimer(string moduleId, Urn alarmUrn)
        {
            _timers.Add((alarmUrn, moduleId), _clock.Schedule(TimeSpan.FromMilliseconds(_panicDelayBeforeRestart), () => _restartAppFunc(_restartBoilerAppUrn)));
        }

        private void ResetTimer(string moduleId, Urn alarmUrn)
        {
            _timers.TryGetValue((alarmUrn, moduleId), out var timer);
            timer?.Dispose();
            _timers.Remove((alarmUrn, moduleId));
        }

        public bool CanHandleProperties(PropertiesChanged propertiesChanged) =>
            propertiesChanged.ModelValues.Select(p => p.Urn).Intersect(_modulesByAlarmUrn.Keys).Any();
    }
}