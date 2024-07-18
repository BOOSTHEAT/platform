using System;
using ImpliciX.Language.Core;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.SharedKernel.Tests
{
    [TestFixture]
    public class SafeEnumParseTests
    {
        [TestCase("bar",MyEnum.Bar)]
        [TestCase("foo",MyEnum.Foo)]
        [TestCase("fIzZ",MyEnum.fizz)]
        public void parse_valid_value(string strValue, MyEnum expected)
        {
            var result = SafeEnum.TryParse<MyEnum>(strValue, ErrFn);
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Value).IsEqualTo(expected);
        }

        [Test]
        public void parse_invalid_value()
        {
            var result = SafeEnum.TryParse<MyEnum>("not a valid input", ErrFn);
            Check.That(result.IsError).IsTrue();
            Check.That(result.Error).IsEqualTo(new MyEnumError("The value: 'not a valid input' is not valid for type MyEnum. Accepted values are: 'Foo','Bar','fizz', the case is not checked."));
        }


        [Test]
        public void non_generic_parse_valid_value()
        {
            var result = SafeEnum.TryParse(typeof(MyEnum), "Fizz", ErrFn);
            Check.That(result.IsSuccess).IsTrue();
            Check.That(result.Value).IsEqualTo(MyEnum.fizz);
        }
        
        [Test]
        public void non_generic_parse_invalid_value()
        {
            var result = SafeEnum.TryParse(typeof(MyEnum), "invalid value", ErrFn);
            Check.That(result.IsError).IsTrue();
        }
        
        

        private static Func<string, Error> ErrFn
        {
            get { return (msg) => new MyEnumError(msg); }
        }
    }

    public enum MyEnum
    {
        Foo,
        Bar,
        fizz,
    }

    public class MyEnumError : Error
    {
        public MyEnumError(string message) : base(nameof(MyEnumError), message)
        {
        }
    }
}