using System;
using System.Linq;
using ImpliciX.Control.Helpers;
using ImpliciX.Control.Tests.Examples.Definition;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.TestsCommon;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.Control.Tests.Fixtures.Helpers
{
    [TestFixture]
    public class PropertiesChangedBufferTests
    {
        [SetUp]
        public void Init()
        {
            EventsHelper.ModelFactory = new ModelFactory(typeof(examples).Assembly);
        }
        
        [Test]
        public void when_one_propertiesChanged_received_new_propertiesChanged_is_return_with_all_properties()
        {
            var sut = new PropertiesChangedBuffer();
            var pc1 = EventsHelper.EventPropertyChanged(new (Urn urn, object value)[]
            {
                (examples.always.prop25, Temperature.Create(0.1f))
            }, TimeSpan.FromMilliseconds(42));

            sut.ReceivedPropertiesChanged(pc1);

            var actual = sut.ReleasePropertiesChanged();
            Check.That(actual.ModelValues).ContainsExactly(
                Property<Temperature>.Create(examples.always.prop25, Temperature.Create(0.1f), TimeSpan.Zero));
            Check.That(actual.At).IsEqualTo(pc1.At);
        }

        [Test]
        public void when_two_propertiesChanged_received_new_propertiesChanged_is_return_with_all_properties()
        {
            var sut = new PropertiesChangedBuffer();
            var pc1 = EventsHelper.EventPropertyChanged(new (Urn urn, object value)[]
            {
                (examples.always.prop25, Temperature.Create(0.1f))
            }, TimeSpan.FromMilliseconds(19));

            var pc2 = EventsHelper.EventPropertyChanged(new (Urn urn, object value)[]
            {
                (examples.always.xprop, Literal.Create("Yolo"))
            }, TimeSpan.FromMilliseconds(42));

            sut.ReceivedPropertiesChanged(pc1);
            sut.ReceivedPropertiesChanged(pc2);

            var actual = sut.ReleasePropertiesChanged();
            Check.That(actual.ModelValues.ToArray().Count()).IsEqualTo(2);
            Check.That(actual.ModelValues.ToArray()).Contains(
                Property<Temperature>.Create(examples.always.prop25, Temperature.Create(0.1f), TimeSpan.Zero),
                Property<Literal>.Create(examples.always.xprop, Literal.Create("Yolo"), TimeSpan.Zero));
            Check.That(actual.At).IsEqualTo(pc2.At);
        }
        
        [Test]
        public void when_duplicated_propertiesChanged_received_new_propertiesChanged_is_return_with_last_properties()
        {
            var sut = new PropertiesChangedBuffer();
            var pc1 = EventsHelper.EventPropertyChanged(new (Urn urn, object value)[]
            {
                (examples.always.prop25, Temperature.Create(0.1f))
            }, TimeSpan.FromMilliseconds(19));

            var pc2 = EventsHelper.EventPropertyChanged(new (Urn urn, object value)[]
            {
                (examples.always.xprop, Literal.Create("Yolo"))
            }, TimeSpan.FromMilliseconds(42));

            var pc3 = EventsHelper.EventPropertyChanged(new (Urn urn, object value)[]
            {
                (examples.always.xprop, Literal.Create("Yolo2"))
            }, TimeSpan.FromMilliseconds(63));

            sut.ReceivedPropertiesChanged(pc1);
            sut.ReceivedPropertiesChanged(pc2);
            sut.ReceivedPropertiesChanged(pc3);

            var actual = sut.ReleasePropertiesChanged();
            Check.That(actual.ModelValues.ToArray().Count()).IsEqualTo(2);
            Check.That(actual.ModelValues.ToArray()).Contains(
                Property<Temperature>.Create(examples.always.prop25, Temperature.Create(0.1f), TimeSpan.Zero),
                Property<Literal>.Create(examples.always.xprop, Literal.Create("Yolo2"), TimeSpan.FromMilliseconds(1)));
            Check.That(actual.At).IsEqualTo(pc3.At);
        }

        [Test]
        public void after_buffer_release_should_return_empty()
        {
            var sut = new PropertiesChangedBuffer();
            var pc1 = EventsHelper.EventPropertyChanged(new (Urn urn, object value)[]
            {
                (examples.always.prop25, Temperature.Create(0.1f))
            }, TimeSpan.FromMilliseconds(42));
            
            sut.ReceivedPropertiesChanged(pc1); 
            sut.ReleasePropertiesChanged();
            
            var actual = sut.ReleasePropertiesChanged().ModelValues.ToArray();
            Check.That(actual.Count()).IsEqualTo(0);
        }
    }
}