using System;
using System.Collections.Generic;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Motors.Controllers.Board;
using ImpliciX.Motors.Controllers.Definitions;
using ImpliciX.Motors.Controllers.Domain;
using ImpliciX.Motors.Controllers.Tests.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Motors.Controllers.Tests
{
    [TestFixture]
    public class DecodeFunctionsTests
    {
        private static Motor[] allMotors = MotorsSlave.CreateAllMotorNodes(test_model.motors.Nodes);

        private List<ReadDefinition> Sut { get; set; }

        [SetUp]
        public void SetUp()
        {
            var motorsMapping = new RegistersMap(allMotors);
            Sut = motorsMapping.MotorMapDefinitions;
        }

        private Dictionary<string, (string, Result<float>)> decode_BPO_test_cases =
            new Dictionary<string, (string, Result<float>)>()
            {
                {"Normal", ("200", Result<float>.Create(200f))},
                {"Error", ("Grouink", Result<float>.Create(new Error("", "")))},
            };

        [TestCase("Normal")]
        [TestCase("Error")]
        public void decode_BPO_test(string testCase)
        {
            AssertDecodeFunction(decode_BPO_test_cases[testCase], RegistersMap.ToWatt);
        }


        private Dictionary<string, (string, Result<float>)> decode_BSP_test_cases =
            new Dictionary<string, (string, Result<float>)>()
            {
                {"Normal", ("200", Result<float>.Create(200f))},
                {"Error", ("Grouink", Result<float>.Create(new Error("", "")))},
            };

        [TestCase("Normal")]
        [TestCase("Error")]
        public void decode_BSP_test(string testCase)
        {
            AssertDecodeFunction(decode_BSP_test_cases[testCase],  RegistersMap.ToRotationalSpeed);
        }

        private Dictionary<string, (string, Result<float>)> decode_DTE_test_cases =
            new Dictionary<string, (string, Result<float>)>()
            {
                {"Normal", ("200", Result<float>.Create(293.15f))},
                {"Error", ("Grouink", Result<float>.Create(new Error("", "")))},
            };

        [TestCase("Normal")]
        [TestCase("Error")]
        public void decode_DTE_test(string testCase)
        {
            AssertDecodeFunction(decode_DTE_test_cases[testCase], RegistersMap.TenthOfDegreeToKelvin);
        }

        private Dictionary<string, (string, Result<float>)> decode_OCU_test_cases =
            new Dictionary<string, (string, Result<float>)>()
            {
                {"Normal", ("200", Result<float>.Create(0.2f))},
                {"Error", ("Grouink", Result<float>.Create(new Error("", "")))},
            };

        [TestCase("Normal")]
        [TestCase("Error")]
        public void decode_OCU_test(string testCase)
        {
            AssertDecodeFunction(decode_OCU_test_cases[testCase], RegistersMap.MilliAmpereToAmpere);
        }

        private Dictionary<string, (string input, Result<MotorCurrent> expected)> decode_STA_current_test_cases =
            new Dictionary<string, (string, Result<MotorCurrent>)>()
            {
                {"Normal", ("0", Result<MotorCurrent>.Create(MotorCurrent.Normal))},
                {"SoftwareDisjunction", ("16", Result<MotorCurrent>.Create(MotorCurrent.SoftwareDisjunction))},
                {"HardwareDisjunction1", ("32", Result<MotorCurrent>.Create(MotorCurrent.HardwareDisjunction))},
                {"HardwareDisjunction2", ("48", Result<MotorCurrent>.Create(MotorCurrent.HardwareDisjunction))},
                {"DeRating", ("8", Result<MotorCurrent>.Create(MotorCurrent.DeRating))},
                {"Error", ("Grouink", Result<MotorCurrent>.Create(new Error("", "")))},
            };

        [TestCase("Normal")]
        [TestCase("SoftwareDisjunction")]
        [TestCase("HardwareDisjunction1")]
        [TestCase("HardwareDisjunction2")]
        [TestCase("DeRating")]
        [TestCase("Error")]
        public void decode_STA_current_test(string testCase)
        {

            AssertDecodeFunction(decode_STA_current_test_cases[testCase], RegistersMap.ToCurrentOperationalState);
        }

        private Dictionary<string, (string input, Result<MotorVoltage> expected)> decode_STA_voltage_test_cases =
            new Dictionary<string, (string, Result<MotorVoltage>)>()
            {
                {"Normal", ("0", Result<MotorVoltage>.Create(MotorVoltage.Normal))},
                {"BrakingLimitation", ("4", Result<MotorVoltage>.Create(MotorVoltage.BrakingLimitation))},
                {"UnderVoltage", ("1", Result<MotorVoltage>.Create(MotorVoltage.UnderVoltage))},
                {"OverVoltage", ("6", Result<MotorVoltage>.Create(MotorVoltage.OverVoltage))},
                {"Error", ("Grouink", Result<MotorVoltage>.Create(new Error("", "")))},
            };

        [TestCase("Normal")]
        [TestCase("BrakingLimitation")]
        [TestCase("UnderVoltage")]
        [TestCase("OverVoltage")]
        [TestCase("Error")]
        public void decode_STA_voltage_test(string testCase)
        {
            AssertDecodeFunction(decode_STA_voltage_test_cases[testCase], RegistersMap.ToVoltageOperationalState);
        }

        private Dictionary<string, (string, Result<float>)> decode_SVO_test_cases =
            new Dictionary<string, (string, Result<float>)>()
            {
                {"Normal", ("200", Result<float>.Create(0.2f))},
                {"Error", ("Grouink", Result<float>.Create(new Error("", "")))},
            };

        [TestCase("Normal")]
        [TestCase("Error")]
        public void decode_SVO_test(string testCase)
        {
            AssertDecodeFunction(decode_SVO_test_cases[testCase], RegistersMap.MilliVoltToVolt);
        }

        private void AssertDecodeFunction<T>((string input, Result<T> expected) testCase, Func<string, Result<T>> decodeFunction)
        {
            decodeFunction(testCase.input).Tap(
                error => Check.That(error).IsEqualTo(testCase.expected.Error),
                value => Check.That(value).IsEqualTo(testCase.expected.Value)
            );
        }
    }
}