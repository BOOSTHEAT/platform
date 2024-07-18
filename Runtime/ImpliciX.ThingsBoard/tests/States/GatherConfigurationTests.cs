using System;
using ImpliciX.Language;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.ThingsBoard.States;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.ThingsBoard.Tests.States
{
  public class GatherConfigurationTests
  {
    [Test]
    public void nominal_case()
    {
      var context = new Context("the-app", new ThingsBoardSettings { GlobalRetries = 3 });
      var model = new ThingsBoardModuleDefinition
      {
        Connection =
        {
          Host = PropertyUrn<Literal>.Build("Host"),
          AccessToken = PropertyUrn<Literal>.Build("AccessToken")
        }
      };
      var sut = Runner.CreateWithSingleState(context, new GatherConfiguration(model));

      Check.That(
        sut.Handle(CreatePropertyChange("Host", Literal.Create("hostname:port")))
      ).IsEmpty();
      Check.That(sut.Context.Host).IsEqualTo("hostname:port");

      Check.That(
        sut.Handle(CreatePropertyChange("AccessToken", Literal.Create("pgqjTSZDngMDFGnjngMDFgl")))
      ).HasOneElementOnly().Which.IsInstanceOf<GatherConfiguration.GatheringComplete>();
      Check.That(sut.Context.AccessToken).IsEqualTo("pgqjTSZDngMDFGnjngMDFgl");
    }

    private PropertiesChanged CreatePropertyChange<T>(string urn, T value) =>
      PropertiesChanged.Create(new []
      {
        Property<T>.Create(PropertyUrn<T>.Build(urn), value, TimeSpan.Zero)
      }, TimeSpan.Zero);
  }
}