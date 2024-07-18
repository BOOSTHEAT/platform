using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.RuntimeFoundations.Tests.FactoryTests.ValueObjects
{
    [ValueObject]
    public struct DummyValueObject : IEquatable<DummyValueObject>
    {
        public string Foo { get; }
        public int Bar { get; }

        public Result<DummyValueObject> FromString()=>throw new NotSupportedException();
        
        public DummyValueObject(string foo, int bar)
        {
            Foo = foo;
            Bar = bar;
        }

        public override string ToString()
        {
            return $"{nameof(Foo)}: {Foo}, {nameof(Bar)}: {Bar}";
        }

        public bool Equals(DummyValueObject other)
        {
            return Foo == other.Foo && Bar == other.Bar;
        }

        public override bool Equals(object obj)
        {
            return obj is DummyValueObject other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Foo, Bar);
        }

        public static bool operator ==(DummyValueObject left, DummyValueObject right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DummyValueObject left, DummyValueObject right)
        {
            return !left.Equals(right);
        }
    }
}