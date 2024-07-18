using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Alarms;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Alarms
{
    public class AlarmsDefinitions
    {
        public AlarmsDefinitions(IEnumerable<Alarm> declarations,
            AlarmSettings alarmSettings)
        {
            AlarmActivationComputation = new List<Func<ModelFactory, TimeSpan, IDataModelValue>>();
            AlarmStateComputation =
                new Dictionary<Urn, List<Func<IDataModelValue, ModelFactory, TimeSpan, Option<DomainEvent>>>>();
            AlarmResetCommand = new Dictionary<Urn, List<Func<ModelFactory, TimeSpan, IEnumerable<DomainEvent>>>>();
            AlarmDeviceComputation =
                new Dictionary<Urn, List<Func<SlaveCommunicationOccured, ModelFactory, TimeSpan, IEnumerable<DomainEvent>>>>();
            foreach (var declaration in declarations)
            {
                switch (declaration.Kind)
                {
                    case Alarm.AlarmKind.Auto:
                        var alarmAutoComputation = new AlarmAutoComputation(declaration.Node);
                        AlarmActivationComputation.Add(alarmAutoComputation.Activate);
                        Add(AlarmStateComputation, declaration.Dependencies[0], alarmAutoComputation.ComputeAlarmState);
                        break;
                    case Alarm.AlarmKind.Communication:
                        var board = declaration.Dependencies[0];
                        var maxErrors = GetMaxErrors(alarmSettings, board);
                        var alarmDeviceComputation = new AlarmDeviceComputation(maxErrors, declaration.Node.state);
                        AlarmActivationComputation.Add(alarmDeviceComputation.Activate);
                        Add(AlarmDeviceComputation, board, alarmDeviceComputation.OnSlaveCommunicationOccured);
                        break;
                    case Alarm.AlarmKind.Manual:
                        var alarmManualComputation =
                            new AlarmManualComputation(declaration.Node, declaration.Dependencies[2]);
                        AlarmActivationComputation.Add(alarmManualComputation.Activate);
                        Add(AlarmStateComputation, declaration.Dependencies[0], alarmManualComputation.ComputeAlarmState);
                        Add(AlarmStateComputation, declaration.Dependencies[1], alarmManualComputation.ComputeAlarmReset);
                        Add(AlarmResetCommand, declaration.Node._reset, alarmManualComputation.OnResetCommand);
                        break;
                    case Alarm.AlarmKind.Trigger:
                        var alarmDataTriggerComputation = new AlarmTriggerComputation(declaration.Node, declaration.Triggers.Predicates);
                        AlarmActivationComputation.Add(alarmDataTriggerComputation.Activate);
                        Add(AlarmStateComputation, declaration.Triggers.Dependency, alarmDataTriggerComputation.OnFunctionalState);
                        Add(AlarmStateComputation, declaration.Node.settings.presence, alarmDataTriggerComputation.OnAlarmPresence);
                        Add(AlarmResetCommand, declaration.Node._reset, alarmDataTriggerComputation.OnResetCommand);
                        break;
                    case Alarm.AlarmKind.Measure:
                        var alarmMeasureComputation = new AlarmMeasureComputation(declaration.Node);
                        AlarmActivationComputation.Add(alarmMeasureComputation.Activate);
                        Add(AlarmStateComputation, declaration.Dependencies[0], alarmMeasureComputation.OnSensorStatus);
                        Add(AlarmStateComputation, declaration.Node.settings.presence, alarmMeasureComputation.OnAlarmPresence);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private static int GetMaxErrors(AlarmSettings alarmSettings, Urn board)
        {
            var consecutiveErrors = alarmSettings.ConsecutiveSlaveCommunicationErrorsBeforeFailure;
            var boardOverride = consecutiveErrors.Override?.Where(o => o.Slave == board).ToArray();
            return boardOverride != null && boardOverride.Any() ? boardOverride.First().Value : consecutiveErrors.Default;
        }

        private static void Add<T>(Dictionary<Urn, List<T>> dict, Urn key, T value)
        {
            if (!dict.ContainsKey(key))
                dict[key] = new List<T>();
            dict[key].Add(value);
        }
        
        public List<Func<ModelFactory, TimeSpan, IDataModelValue>> AlarmActivationComputation { get; }

        public Dictionary<Urn, List<Func<IDataModelValue, ModelFactory, TimeSpan, Option<DomainEvent>>>> AlarmStateComputation
        {
            get;
        }

        public Dictionary<Urn, List<Func<SlaveCommunicationOccured, ModelFactory, TimeSpan, IEnumerable<DomainEvent>>>>
            AlarmDeviceComputation { get; }

        public Dictionary<Urn, List<Func<ModelFactory, TimeSpan, IEnumerable<DomainEvent>>>> AlarmResetCommand { get; }
    }
}