using System;
using ImpliciX.Harmony.States;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using NFluent;
using NUnit.Framework;
using TimeZone = ImpliciX.Language.Model.TimeZone;

namespace ImpliciX.Harmony.Tests.States
{
    public class GatherConfigurationTests
    {
        [Test]
        public void nominal_case()
        {
            var context = new Context("the-app", "the.dps.uri", 3, TimeSpan.FromSeconds(20));
            var sut = Runner.CreateWithSingleState(context, new GatherConfiguration(new HarmonyModuleDefinition
            {
                DeviceId = PropertyUrn<Literal>.Build("DeviceId"),
                IDScope = PropertyUrn<Literal>.Build("ID Scope"),
                SymmetricKey = PropertyUrn<Literal>.Build("SymKey"),
                DeviceSerialNumber = PropertyUrn<Literal>.Build("BoilerSerial"),
                ReleaseVersion = PropertyUrn<SoftwareVersion>.Build("ReleaseVersion"),
                UserTimeZone = PropertyUrn<TimeZone>.Build("TimeZone")
            }));

            Check.That(
                sut.Handle(CreatePropertyChange("DeviceId", Literal.Create("the_device_id")))
            ).IsEmpty();
            Check.That(sut.Context.DeviceId).IsEqualTo("the_device_id");

            Check.That(
                sut.Handle(CreatePropertyChange("ID Scope", Literal.Create("Two000B42")))
            ).IsEmpty();
            Check.That(sut.Context.DpsSettings.IdScope).IsEqualTo("Two000B42");

            Check.That(
                sut.Handle(CreatePropertyChange("SymKey", Literal.Create("pgqjMGFDngMDFGnjngMDFgl")))
            ).IsEmpty();
            Check.That(sut.Context.IotHubSettings.SymmetricKey).IsEqualTo("pgqjMGFDngMDFGnjngMDFgl");

            Check.That(
                sut.Handle(CreatePropertyChange("BoilerSerial", Literal.Create("FOO")))
            ).IsEmpty();
            Check.That(sut.Context.SerialNumber).IsEqualTo("FOO");

            Check.That(
                sut.Handle(CreatePropertyChange("TimeZone", TimeZone.Europe__Paris))
            ).IsEmpty();
            Check.That(sut.Context.UserTimeZone).IsEqualTo("Europe__Paris");

            Check.That(
                sut.Handle(CreatePropertyChange("ReleaseVersion", SoftwareVersion.Create(1, 2, 3, 4)))
            ).HasOneElementOnly().Which.IsInstanceOf<GatherConfiguration.GatheringComplete>();
            Check.That(sut.Context.ReleaseVersion).IsEqualTo("1.2.3.4");
        }

        private PropertiesChanged CreatePropertyChange<T>(string urn, T value) =>
            PropertiesChanged.Create(new[]
            {
                Property<T>.Create(PropertyUrn<T>.Build(urn), value, TimeSpan.Zero)
            }, TimeSpan.Zero);
    }
}