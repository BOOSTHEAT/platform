using System;
using System.Linq;
using ImpliciX.Control.Helpers;
using ImpliciX.Language.Control;
using ImpliciX.Language.Model;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Control.Tests.Fixtures.Helpers
{
    [TestFixture]
    public class ConditionsTests
    {
        [TestCase(new []{1f,2f},true)]
        [TestCase(new []{3f,2f},false)]
        [TestCase(new []{-1f,-2f},false)]
        [TestCase(new []{0f,0f},false)]
        [TestCase(new []{-2f,1f},true)]
        public void lower_than(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.Lt, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1f, 1f, 0f},true)]
        [TestCase(new []{3f, 2f, 0f},false)]
        [TestCase(new []{-1f,-2f, 2f},true)]
        [TestCase(new []{0f, 3f, 2f},false)]
        public void equality_with_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.EqWithEpsilon, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1f, 1f, 0f},true)]
        [TestCase(new []{3f, 2f, 0f},false)]
        [TestCase(new []{-1f,-1.25f, 0.5f},true)]
        [TestCase(new []{0f, 3f, 0.2f},false)]
        public void equality_with_tolerance(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.EqWithTolerance, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1f, 1f, 0f},false)]
        [TestCase(new []{3f, 2f, 0f},true)]
        [TestCase(new []{-1f,-1.25f, 0.5f},false)]
        [TestCase(new []{0f, 3f, 0.2f},true)]
        public void greater_with_tolerance(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.NotEqWithTolerance, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1f, 0.5f, 0.6f},true)]
        [TestCase(new []{1f, 0.5f, 0.5f},false)]
        [TestCase(new []{1f, 0.5f, 0.4f},false)]
        [TestCase(new []{-1f, -1.5f, 0.6f},true)]
        [TestCase(new []{-1f, -1.5f, 0.5f},false)]
        [TestCase(new []{-1f, -1.5f, 0.4f},false)]
        public void lower_than_plus_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.LtPlusEpsilon, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1f, 1.5f, 0.6f},false)]
        [TestCase(new []{1f, 1.5f, 0.5f},false)]
        [TestCase(new []{1f, 1.5f, 0.4f},true)]
        [TestCase(new []{-2f, -1.5f, 0.6f},false)]
        [TestCase(new []{-2f, -1.5f, 0.5f},false)]
        [TestCase(new []{-2f, -1.5f, 0.4f},true)]
        public void lower_than_minus_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.LtMinusEpsilon, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1f, 0.5f, 0.6f},true)]
        [TestCase(new []{1f, 0.5f, 0.5f},true)]
        [TestCase(new []{1f, 0.5f, 0.4f},false)]
        [TestCase(new []{-1f, -1.5f, 0.6f},true)]
        [TestCase(new []{-1f, -1.5f, 0.5f},true)]
        [TestCase(new []{-1f, -1.5f, 0.4f},false)]
        public void lower_than_or_eq_plus_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.LtOrEqPlusEpsilon, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1f, 1.5f, 0.6f},false)]
        [TestCase(new []{1f, 1.5f, 0.5f},true)]
        [TestCase(new []{1f, 1.5f, 0.4f},true)]
        [TestCase(new []{-2f, -1.5f, 0.6f},false)]
        [TestCase(new []{-2f, -1.5f, 0.5f},true)]
        [TestCase(new []{-2f, -1.5f, 0.4f},true)]
        public void lower_than_or_eq_minus_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.LtOrEqMinusEpsilon, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1.5f, 1f, 0.6f},false)]
        [TestCase(new []{1.5f, 1f, 0.5f},false)]
        [TestCase(new []{1.5f, 1f, 0.4f},true)]
        [TestCase(new []{-1.5f, -2f, 0.6f},false)]
        [TestCase(new []{-1.5f, -2f, 0.5f},false)]
        [TestCase(new []{-1.5f, -2f, 0.4f},true)]
        public void greater_than_plus_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.GtPlusEpsilon, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1.5f, 2.0f, 0.6f},true)]
        [TestCase(new []{1.5f, 2.0f, 0.5f},false)]
        [TestCase(new []{1.5f, 2.0f, 0.4f},false)]
        [TestCase(new []{-1.5f, -1f, 0.6f},true)]
        [TestCase(new []{-1.5f, -1f, 0.5f},false)]
        [TestCase(new []{-1.5f, -1f, 0.4f},false)]
        public void greater_than_minus_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.GtMinusEpsilon, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1.5f, 1f, 0.6f},false)]
        [TestCase(new []{1.5f, 1f, 0.5f},true)]
        [TestCase(new []{1.5f, 1f, 0.4f},true)]
        [TestCase(new []{-1.5f, -2f, 0.6f},false)]
        [TestCase(new []{-1.5f, -2f, 0.5f},true)]
        [TestCase(new []{-1.5f, -2f, 0.4f},true)]
        public void greater_than_or_eq_plus_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.GtOrEqPlusEpsilon, operands)).IsEqualTo(expected);
        }

        [TestCase(new []{1.5f, 2.0f, 0.6f},true)]
        [TestCase(new []{1.5f, 2.0f, 0.5f},true)]
        [TestCase(new []{1.5f, 2.0f, 0.4f},false)]
        [TestCase(new []{-1.5f, -1f, 0.6f},true)]
        [TestCase(new []{-1.5f, -1f, 0.5f},true)]
        [TestCase(new []{-1.5f, -1f, 0.4f},false)]
        public void greater_than_or_eq_minus_epsilon(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.GtOrEqMinusEpsilon, operands)).IsEqualTo(expected);
        }
        
        [TestCase(new []{1f,2f},false)]
        [TestCase(new []{3f,2f},true)]
        [TestCase(new []{-1f,-2f},true)]
        [TestCase(new []{0f,0f},false)]
        [TestCase(new []{-2f,1f},false)]
        public void greater_than(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.Gt, operands)).IsEqualTo(expected);
        }
        
        [TestCase(new []{1f,2f},false)]
        [TestCase(new []{3f,2f},true)]
        [TestCase(new []{-1f,-2f},true)]
        [TestCase(new []{0f,0f},true)]
        [TestCase(new []{-2f,1f},false)]
        public void greater_or_equal_to(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.GtOrEqTo, operands)).IsEqualTo(expected);
        }
        
        [TestCase(new []{1f,2f},true)]
        [TestCase(new []{3f,2f},false)]
        [TestCase(new []{-1f,-2f},false)]
        [TestCase(new []{0f,0f},true)]
        [TestCase(new []{-2f,1f},true)]
        public void lower_or_equal_to(float[] input, bool expected)
        {
            var operands = input.Select(f => new FloatValue(f)).Cast<object>().ToArray();
            Check.That(Conditions.Operator(Op.LtOrEqTo, operands)).IsEqualTo(expected);
        }
        
        [TestCase(new object[]{StateTest.A,StateTest.A},true)]
        [TestCase(new object[]{StateTest.A,StateTest.B},false)]
        [TestCase(new object[]{StateTest.B,StateTest.B},true)]
        [TestCase(new object[]{StateTest.B,StateTest.A},false)]
        public void Is(object[] input, bool expected)
        {
            Check.That(Conditions.Operator(Op.Is, input)).IsEqualTo(expected);
        }
        
        [TestCase(new object[]{StateTest.A,StateTest.A},false)]
        [TestCase(new object[]{StateTest.A,StateTest.B},true)]
        [TestCase(new object[]{StateTest.B,StateTest.B},false)]
        [TestCase(new object[]{StateTest.B,StateTest.A},true)]
        public void IsNot(object[] input, bool expected)
        {
            Check.That(Conditions.Operator(Op.IsNot, input)).IsEqualTo(expected);
        }

        [TestCase(new []{StateTest.A} , StateTest.A, true)]
        [TestCase(new []{StateTest.A} , StateTest.B, false)]
        [TestCase(new []{StateTest.A, StateTest.B}, StateTest.B,true)]
        [TestCase(new []{StateTest.A, StateTest.B}, StateTest.A,true)]
        public void InState(StateTest[] statesSet, StateTest checkedState, bool expected)
        {
            var chain = EnumSequence.Create(statesSet.Cast<Enum>().ToArray());
            Check.That(Conditions.Operator(Op.InState, new object[]{chain, checkedState})).IsEqualTo(expected);
        }
    }

    public enum StateTest
    {
        A,
        B,
    }

    public class FloatValue : IFloat
    {
        private readonly float _v;

        public FloatValue(float v)
        {
            _v = v;
        }

        public float ToFloat() => _v;
    }
}