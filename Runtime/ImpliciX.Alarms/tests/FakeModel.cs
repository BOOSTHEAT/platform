using System;
using System.Collections.Generic;
using ImpliciX.Language.Alarms;
using ImpliciX.Language.Model;

namespace ImpliciX.Alarms.Tests
{
    [ValueObject]
    public enum FakePump
    {
        running = 1,
        Undervoltage_warning = -1,
        RPM_sensor_fault = -2,
        Undervoltage_stop = -3,
        Rotor_blocked = -4,
        standby_stop = 0
    }

    public class fake : RootModelNode
    {
        public static MeasureNode<Temperature> production_main_circuit_supply_temperature { get; }
        public static AlarmNode alarms_C061 { get; }
        public static ManualAlert<Temperature> production_main_circuit_supply_temperature_high_temperature { get; }
        public static AlarmNode alarms_C029 { get; }
        public static ManualAlert<RotationalSpeed> production_auxiliary_burner_fan_starting_instability { get; }
        public static ManualAlert<RotationalSpeed> whirlpool_activation { get; }
        public static HardwareAndSoftwareDeviceNode devices_bh20_iu { get; }
        public static AlarmNode alarms_C063 { get; }
        public static HardwareAndSoftwareDeviceNode devices_bh20_eu { get; }
        public static AlarmNode alarms_C064 { get; }
        public static HardwareAndSoftwareDeviceNode devices_bh20_heat_pump { get; }
        public static AlarmNode alarms_C065 { get; }
        public static AlarmNode alarms_C075 { get; }
        public static AlarmNode alarms_C076 { get; }
        public static AlarmNode alarms_C077 { get; }
        public static AlarmNode alarms_C666 { get; }
        public static MeasureNode<FakePump> production_heat_pump_mpg_pump_functional_state { get; }
        public static MeasureNode<Temperature> service_dhw_supply_temperature { get; }

        public fake() : base(nameof(fake))
        {
        }

        static fake()
        {
            var root = new fake();
            alarms_C029 = new AlarmNode(nameof(alarms_C029), root);
            alarms_C061 = new AlarmNode(nameof(alarms_C061), root);
            alarms_C063 = new AlarmNode(nameof(alarms_C063), root);
            alarms_C064 = new AlarmNode(nameof(alarms_C064), root);
            alarms_C065 = new AlarmNode(nameof(alarms_C065), root);
            alarms_C075 = new AlarmNode(nameof(alarms_C075), root);
            alarms_C076 = new AlarmNode(nameof(alarms_C076), root);
            alarms_C077 = new AlarmNode(nameof(alarms_C077), root);
            alarms_C666 = new AlarmNode(nameof(alarms_C666), root);
            production_main_circuit_supply_temperature =
                new MeasureNode<Temperature>(Urn.BuildUrn(nameof(production_main_circuit_supply_temperature)), root);
            production_main_circuit_supply_temperature_high_temperature =
                new ManualAlert<Temperature>(
                    Urn.BuildUrn(nameof(production_main_circuit_supply_temperature_high_temperature)), root);
            production_auxiliary_burner_fan_starting_instability =
                new ManualAlert<RotationalSpeed>(
                    Urn.BuildUrn(nameof(production_auxiliary_burner_fan_starting_instability)), root);
            whirlpool_activation = new ManualAlert<RotationalSpeed>(Urn.BuildUrn(nameof(whirlpool_activation)), root);
            production_heat_pump_mpg_pump_functional_state =
                new MeasureNode<FakePump>(Urn.BuildUrn(nameof(production_heat_pump_mpg_pump_functional_state)), root);
            service_dhw_supply_temperature =
                new MeasureNode<Temperature>(Urn.BuildUrn(nameof(service_dhw_supply_temperature)), root);
            devices_bh20_iu = new HardwareAndSoftwareDeviceNode(nameof(devices_bh20_iu), root);
            devices_bh20_eu = new HardwareAndSoftwareDeviceNode(nameof(devices_bh20_eu), root);
            devices_bh20_heat_pump = new HardwareAndSoftwareDeviceNode(nameof(devices_bh20_heat_pump), root);
        }
    }

    public class AllAlarms
    {
        public static readonly IEnumerable<Alarm> Declarations = new[]
        {
            Alarm.Auto(fake.alarms_C666, fake.whirlpool_activation.public_state),
            Alarm.Manual(fake.alarms_C029,
                fake.production_main_circuit_supply_temperature_high_temperature.public_state,
                fake.production_main_circuit_supply_temperature_high_temperature.ready_to_reset,
                fake.production_main_circuit_supply_temperature_high_temperature._reset),
            Alarm.Measure(fake.alarms_C061, fake.production_main_circuit_supply_temperature.status),
            Alarm.Communication(fake.alarms_C063, fake.devices_bh20_iu.Urn),
            Alarm.Communication(fake.alarms_C064, fake.devices_bh20_eu.Urn),
            Alarm.Communication(fake.alarms_C065, fake.devices_bh20_heat_pump.Urn),
            Alarm.Trigger(fake.alarms_C075,
                PumpAlarmTriggers(fake.production_heat_pump_mpg_pump_functional_state.measure, FakePump.Rotor_blocked)),
            Alarm.Trigger(fake.alarms_C076,
                PumpAlarmTriggers(fake.production_heat_pump_mpg_pump_functional_state.measure,
                    FakePump.Undervoltage_stop)),
            Alarm.Measure(fake.alarms_C077, fake.service_dhw_supply_temperature.status),
        };


        private static Triggers PumpAlarmTriggers(Urn triggerUrn, Enum triggerValue) =>
            new Triggers
            {
                Dependency = triggerUrn,
                Predicates = new Func<IDataModelValue, bool>[]
                {
                    v => v.ModelValue().Equals(triggerValue),
                    v => v.ModelValue().Equals(FakePump.standby_stop) || v.ModelValue().Equals(FakePump.running)
                }
            };
    }
}