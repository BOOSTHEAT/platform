using System;
using System.Collections.Generic;
using ImpliciX.Control.Helpers;
using ImpliciX.Language.Control;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using NUnit.Framework;

namespace ImpliciX.Control.Tests.Fixtures.Helpers
{
    [TestFixture]
    public class ConditionsOnDataModelValueObjects
    {

        [TestCase(Op.Lt,typeof(Temperature),"1","2",true)]
        [TestCase(Op.Lt,typeof(DifferentialTemperature),"1","2",true)]
        [TestCase(Op.Lt,typeof(DifferentialPressure),"1","2",true)]
        [TestCase(Op.Lt,typeof(Percentage),"0.1","0.2",true)]
        [TestCase(Op.Lt,typeof(DisplacementQueue),"1","2",true)]
        [TestCase(Op.Lt,typeof(AngularSpeed),"1","2",true)]
        [TestCase(Op.Lt,typeof(Duration),"1","2",true)]
        [TestCase(Op.Lt,typeof(Pressure),"1","2",true)]
        public void should_be_comparable(Op op, Type valueObjectType, string left, string right, bool expected)
        {
            var opLeft = ComparableValueObjects[valueObjectType](left);
            var opRight = ComparableValueObjects[valueObjectType](right);

            Assert.IsTrue(Conditions.Operator(Op.Lt, new[] {opLeft,opRight}));
        }
        
        
        public Dictionary<Type, Func<string,object>> ComparableValueObjects = new Dictionary<Type, Func<string,object>>()
        {
            {typeof(Temperature),CreateValueObject(Temperature.FromString)},
            {typeof(DifferentialTemperature),CreateValueObject(DifferentialTemperature.FromString)},
            {typeof(DifferentialPressure), CreateValueObject(DifferentialPressure.FromString)},
            {typeof(Percentage), CreateValueObject(Percentage.FromString)},
            {typeof(DisplacementQueue), CreateValueObject(DisplacementQueue.FromString)},
            {typeof(AngularSpeed), CreateValueObject(AngularSpeed.FromString)},
            {typeof(RotationalSpeed), CreateValueObject(RotationalSpeed.FromString)},
            {typeof(Duration), CreateValueObject(Duration.FromString)},
            {typeof(Pressure), CreateValueObject(Pressure.FromString)}
        };

        public static Func<string,object> CreateValueObject<T>(Func<string, Result<T>> factory)
        {
            return (v) =>  factory(v).Value;
        }
    }
}