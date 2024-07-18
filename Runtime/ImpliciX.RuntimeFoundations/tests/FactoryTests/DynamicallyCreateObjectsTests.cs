using System;
using System.Globalization;
using System.Threading;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.Elements;
using ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects;
using ImpliciX.SharedKernel.Storage;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RuntimeFoundations.Tests.FactoryTests
{
    [TestFixture]
    public class DynamicallyCreateObjectsTests
    {
        [SetUp]
        public void SetUp()
        {
            TestTime = new TimeSpan(11, 26, 35);
            Sut = new ModelFactory(typeof(DynamicallyCreateObjectsTests).Assembly);
        }

        public TimeSpan TestTime { get; set; }

        public ModelFactory Sut { get; private set; }

        [Test]
        public void create_command_noargs()
        {
          var result1 =  Sut.Create("lightning:interior:SHUTDOWN", ".");
          result1.CheckIsSuccessAnd((o) =>
          {
              var msg = o as Command<NoArg>;
              Check.That(msg).IsNotNull();
              Check.That(msg?.Urn.Value).IsEqualTo(lightning.interior._shutdown);
              Check.That(msg?.Urn).IsInstanceOf<CommandUrn<NoArg>>();
              Check.That(msg?.Arg).IsEqualTo(default(NoArg));
          });
          
          var result2 =  Sut.Create("lightning:interior:RESTART", "yolo");
          result2.CheckIsSuccessAnd((o) =>
          {
              var msg = o as Command<NoArg>;
              Check.That(msg).IsNotNull();
              Check.That(msg?.Urn.Value).IsEqualTo(lightning.interior._restart);
              Check.That(msg?.Urn).IsInstanceOf<CommandUrn<NoArg>>();
              Check.That(msg?.Arg).IsEqualTo(default(NoArg));
          });
        }

        [Test]
        public void create_message_with_enums_as_argument()
        {
            var result = Sut.Create("lightning:interior:kitchen:SWITCH", "on");
            result.CheckIsSuccessAnd((o) =>
            {
                var msg = o as Command<Switch>;
                Check.That(msg).IsNotNull();
                Check.That(msg?.Urn.Value).IsEqualTo(lightning.interior.kitchen._switch);
                Check.That(msg?.Urn).IsInstanceOf<CommandUrn<Switch>>();
                Check.That(msg?.Arg).IsEqualTo(Switch.On);
            });
        }
        
        [Test]
        public void create_message_with_enums_as_argument_invalid_value()
        {
            var result = Sut.Create("lightning:interior:kitchen:SWITCH", "invalid arg");
            Check.That(result.IsError).IsTrue();
        }

        [Test]
        public void create_message_with_struct_value_objects_arguments()
        {
            var result = Sut.Create("lightning:interior:kitchen:TUNE", "56.8");
            result.CheckIsSuccessAnd((o) =>
            {
                var msg = o as Command<Intensity>;
                Check.That(msg).IsNotNull();
                Check.That(msg?.Urn.Value).IsEqualTo(lightning.interior.kitchen._tune);
                Check.That(msg?.Urn).IsInstanceOf<CommandUrn<Intensity>>();
                Check.That(msg?.Arg).IsEqualTo(Intensity.FromFloat(56.8f).Value);
            });
        }
        
        [Test]
        public void create_objects_having_float_values()
        {
            var initialCulture = Thread.CurrentThread.CurrentCulture; 
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            var result = Sut.Create("lightning:interior:kitchen:TUNE", 56.87854f);
            Thread.CurrentThread.CurrentCulture = initialCulture;
            result.CheckIsSuccessAnd((o) =>
            {
                var msg = o as Command<Intensity>;
                Check.That(msg).IsNotNull();
                Check.That(msg?.Urn.Value).IsEqualTo(lightning.interior.kitchen._tune);
                Check.That(msg?.Urn).IsInstanceOf<CommandUrn<Intensity>>();
                Check.That(msg?.Arg).IsEqualTo(Intensity.FromFloat(56.87854f).Value);
            });
            
        }
        
        [Test]
        public void create_message_with_struct_value_objects_invalid_value()
        {
            var result = Sut.Create("lightning:interior:kitchen:TUNE", "not a float");
            Check.That(result.IsError).IsTrue();
        }

        [Test]
        public void create_properties_with_struct_value_objects()
        {
            var result = Sut.Create("lightning:interior:kitchen:consumption", "156.8", TestTime);
            
            result.CheckIsSuccessAnd((o) =>
            {
                var property = o as Property<PowerConsumption>;
                Check.That(property).IsNotNull();
                Check.That(property?.Urn.Value).IsEqualTo(lightning.interior.kitchen.consumption);
                Check.That(property?.Urn).IsInstanceOf<PropertyUrn<PowerConsumption>>();
                Check.That(property?.Value).IsEqualTo(PowerConsumption.FromFloat(156.8f).Value);
                Check.That(property?.At).IsEqualTo(TestTime);

                var nonGenericProp = o as IDataModelValue;
                Check.That(nonGenericProp?.Urn.Value).IsEqualTo(lightning.interior.kitchen.consumption);
                Check.That(nonGenericProp?.ModelValue()).IsEqualTo(PowerConsumption.FromFloat(156.8f).Value);
                Check.That(nonGenericProp?.At).IsEqualTo(TestTime);
            });
        }
        
        [Test]
        public void create_properties_with_struct_value_objects_invalid_value()
        {
            var result = Sut.Create("lightning:interior:kitchen:consumption", "not a float");
            Check.That(result.IsError).IsTrue();
        }
        
        [Test]
        public void create_properties_with_enum_value_objects()
        {
            var result = Sut.Create("lightning:interior:kitchen:lights:1:status", "On", TestTime);
            
            result.CheckIsSuccessAnd((o) =>
            {
                var property = o as Property<LightStatus>;
                Check.That(property).IsNotNull();
                Check.That(property?.Urn).IsEqualTo(lightning.interior.kitchen.lights._1.status);
                Check.That(property?.Urn).IsInstanceOf<PropertyUrn<LightStatus>>();
                Check.That(property?.Value).IsEqualTo(LightStatus.On);
                Check.That(property?.At).IsEqualTo(TestTime);

                var nonGenericProp = o as IDataModelValue;
                Check.That(nonGenericProp?.Urn).IsEqualTo(lightning.interior.kitchen.lights._1.status);
                Check.That(nonGenericProp?.ModelValue()).IsEqualTo(LightStatus.On);
                Check.That(nonGenericProp?.At).IsEqualTo(TestTime);
            });
        }
        
        [Test]
        public void create_properties_with_enum_value_objects_invalid_value()
        {
            var result = Sut.Create("lightning:interior:kitchen:lights:1:status", "not a valid status");
            Check.That(result.IsError).IsTrue();
        }
        
        [Test]
        public void create_settings_with_enum_value_objects()
        {
            var result = Sut.Create("lightning:interior:kitchen:settings:mode", "Manual", TestTime);
            
            result.CheckIsSuccessAnd((o) =>
            {
                var property = o as Property<ControlMode>;
                Check.That(property).IsNotNull();
                Check.That(property?.Urn).IsEqualTo(lightning.interior.kitchen.settings.mode);
                Check.That(property?.Urn).IsInstanceOf<UserSettingUrn<ControlMode>>();
                Check.That(property?.Value).IsEqualTo(ControlMode.Manual);
                Check.That(property?.At).IsEqualTo(TestTime);

                var nonGenericProp = o as IDataModelValue;
                Check.That(nonGenericProp?.Urn).IsEqualTo(lightning.interior.kitchen.settings.mode);
                Check.That(nonGenericProp?.ModelValue()).IsEqualTo(ControlMode.Manual);
                Check.That(nonGenericProp?.At).IsEqualTo(TestTime);
            });
        }
        
        [Test]
        public void create_settings_with_value_objects_composed_of_tuples()
        {
            var hashTime = new TimeSpan(1,2,3);
            var hashValue = new HashValue("lightning:interior:kitchen:compute", new[]{("C0","1.5"),("C1","0.5"),("at","01:02:03")});
            var result = Sut.Create(hashValue); 
            var expected = FunctionDefinition.From(new []{("C0",1.5f),("C1",0.5f)}).GetValueOrDefault();

            
            result.CheckIsSuccessAnd((o) =>
            {
                var property = o as Property<FunctionDefinition>;
                Check.That(property).IsNotNull();
                Check.That(property?.Urn.Value).IsEqualTo(lightning.interior.kitchen.compute);
                Check.That(property?.Urn).IsInstanceOf<PropertyUrn<FunctionDefinition>>();
                Check.That(property?.Value).IsEqualTo(expected);
                Check.That(property?.At).IsEqualTo(hashTime);

                var nonGenericProp = o as IDataModelValue;
                Check.That(nonGenericProp?.Urn.Value).IsEqualTo(lightning.interior.kitchen.compute);
                Check.That(nonGenericProp?.ModelValue()).IsEqualTo(expected);
                Check.That(nonGenericProp?.At).IsEqualTo(hashTime);
            });
        }
        
        [Test]
        public void create_function_definition_with_single_parameter()
        {
            var hashTime = new TimeSpan(1,2,3);
            var hashValue = new HashValue("lightning:interior:kitchen:compute", new[]{("C0","1.5"),("at","01:02:03")});
            var result = Sut.Create(hashValue); 
            var expected = FunctionDefinition.From(new []{("C0",1.5f)}).GetValueOrDefault();

            result.CheckIsSuccessAnd((o) =>
            {
                var property = o as Property<FunctionDefinition>;
                Check.That(property).IsNotNull();
                Check.That(property?.Urn.Value).IsEqualTo(lightning.interior.kitchen.compute);
                Check.That(property?.Urn).IsInstanceOf<PropertyUrn<FunctionDefinition>>();
                Check.That(property?.Value).IsEqualTo(expected);
                Check.That(property?.At).IsEqualTo(hashTime);
            });
        }

        [Test]
        public void create_function_definition_from_string_serialized_definition()
        {
            var hashTime = new TimeSpan(1,2,3);
            var result = Sut.Create("lightning:interior:kitchen:compute","a:1|b:2|c:4", hashTime); 
            var expected = FunctionDefinition.From(new []{("a",1f), ("b",2), ("c",4)}).GetValueOrDefault();

            result.CheckIsSuccessAnd((o) =>
            {
                var property = o as Property<FunctionDefinition>;
                Check.That(property).IsNotNull();
                Check.That(property?.Urn.Value).IsEqualTo(lightning.interior.kitchen.compute);
                Check.That(property?.Urn).IsInstanceOf<PropertyUrn<FunctionDefinition>>();
                Check.That(property?.Value).IsEqualTo(expected);
                Check.That(property?.At).IsEqualTo(hashTime);
            });
        }
        
        
        [Test]
        public void create_command_node_with_success()
        {
            var result = Sut.Create("lightning:interior:kitchen:CLEAN", "A", TestTime);
            result.CheckIsSuccessAnd((o) =>
            {
                var msg = o as Command<Target>;
                Check.That(msg).IsNotNull();
                Check.That(msg?.Urn.Value).IsEqualTo(lightning.interior.kitchen._clean);
                Check.That(msg?.Urn).IsInstanceOf<CommandUrn<Target>>();
                Check.That(msg?.Arg).IsEqualTo(Target.A);
            });
        }
        
        [Test]
        public void create_success_property_of_command_node_with_success()
        {
            var result = Sut.Create("lightning:interior:kitchen:CLEAN:measure", "A", TestTime);
            result.CheckIsSuccessAnd((o) =>
            {
                var prop = o as Property<Target>;
                Check.That(prop).IsNotNull();
                Check.That(prop?.Urn.Value).IsEqualTo(lightning.interior.kitchen._clean.measure);
                Check.That(prop?.Urn).IsInstanceOf<PropertyUrn<Target>>();
                Check.That(prop?.Value).IsEqualTo(Target.A);
            });
        }

        [TestCase("w:h:a:t:e:v:e:r",false)]
        [TestCase("lightning:interior:kitchen:compute",true)]
        [TestCase("lightning:interior:kitchen:CLEAN:measure",true)]
        [TestCase("lightning:interior:kitchen:NOT_EXIST:success",false)]
        [TestCase("lightning:interior:local_private_node:my_secret_temp",true)]
        [TestCase("lightning:interior:not_exist_local_private_node:my_secret_temp",false)]
        public void urn_exists_test(string urn, bool expected)
        {
            var result = Sut.UrnExists(urn);
            Check.That(result).IsEqualTo(expected);
        }
        
        [Test]
        public void create_private_property()
        {
            var result = Sut.Create("lightning:interior:local_private_node:my_secret_temp", "12", TestTime);
            result.CheckIsSuccessAnd((o) =>
            {
                var prop = o as Property<Temperature>;
                Check.That(prop).IsNotNull();
                Check.That(prop?.Urn.Value).IsEqualTo(lightning.interior._private<local_private_node>().my_secret_temp);
                Check.That(prop?.Urn).IsInstanceOf<PropertyUrn<Temperature>>();
                Check.That(prop?.Value.Degrees).IsEqualTo(12.0f);
            });
        }
        
        [Test]
        public void create_private_property_from_vaild_urn_and_value_object()
        {
            var result = Sut.Create(lightning.interior._private<local_private_node>()._dummy_cmd.measure, new DummyValueObject("foo",1), TestTime);
            result.CheckIsSuccessAnd((o) =>
            {
                var prop = o as Property<DummyValueObject>;
                Check.That(prop).IsNotNull();
                Check.That(prop?.Urn.Value).IsEqualTo(lightning.interior._private<local_private_node>()._dummy_cmd.measure);
                Check.That(prop?.Urn).IsInstanceOf<PropertyUrn<DummyValueObject>>();
                Check.That(prop?.Value).IsEqualTo(new DummyValueObject("foo",1));
            });
        }

        [Test]
        public void create_version_settings_node()
        {
            var result = Sut.Create("lightning:interior:kitchen:settings:coef", 0.23f, TestTime);
            
            result.CheckIsSuccessAnd((o) =>
            {
                var dmv = o as IDataModelValue;
                Check.That(dmv?.Urn).IsEqualTo(lightning.interior.kitchen.settings.coef);
                Check.That(dmv?.ModelValue()).IsEqualTo(Percentage.FromFloat(0.23f).Value);
                Check.That(dmv?.At).IsEqualTo(TestTime);
            });
        }

        [Test]
        public void create_user_settings_node()
        {
            var result = Sut.Create("lightning:interior:kitchen:settings:mode", "Manual", TestTime);
            
            result.CheckIsSuccessAnd((o) =>
            {
                var dmv = o as IDataModelValue;
                Check.That(dmv?.Urn).IsEqualTo(lightning.interior.kitchen.settings.mode);
                Check.That(dmv?.ModelValue()).IsEqualTo(ControlMode.Manual);
                Check.That(dmv?.At).IsEqualTo(TestTime);
            });
        }

        [Test]
        public void create_factory_settings_node()
        {
            var result = Sut.Create("lightning:interior:kitchen:settings:defaultMode", "Manual", TestTime);
            
            result.CheckIsSuccessAnd((o) =>
            {
                var dmv = o as IDataModelValue;
                Check.That(dmv?.Urn).IsEqualTo(lightning.interior.kitchen.settings.defaultMode);
                Check.That(dmv?.ModelValue()).IsEqualTo(ControlMode.Manual);
                Check.That(dmv?.At).IsEqualTo(TestTime);
            });
        }

        [Test]
        public void get_all_urns()
        {
            var urns = Sut.GetAllUrns();
            Check.That(urns).IsEqualTo(
            new []
            {
                new lightning().Urn,
                lightning.interior.Urn,
                lightning.interior.kitchen.Urn,
                lightning.interior.kitchen._clean,
                lightning.interior.kitchen._clean.measure,
                lightning.interior.kitchen._clean.status,
                lightning.interior.kitchen.compute,
                lightning.interior.kitchen.consumption,
                lightning.interior.kitchen.lights.Urn,
                lightning.interior.kitchen.lights._1.Urn,
                lightning.interior.kitchen.lights._1.status,
                lightning.interior.kitchen.lights._2.Urn,
                lightning.interior.kitchen.lights._2.status,
                lightning.interior.kitchen.settings.Urn,
                lightning.interior.kitchen.settings.coef,
                lightning.interior.kitchen.settings.defaultMode,
                lightning.interior.kitchen.settings.mode,
                lightning.interior.kitchen._switch,
                lightning.interior.kitchen._tune,
                lightning.interior._restart,
                lightning.interior._shutdown
            });
        }
    }
}