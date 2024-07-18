using System;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Modules;
using NFluent;
using NUnit.Framework;

namespace ImpliciX.RuntimeFoundations.Tests
{
  [TestFixture]
  public class ModulePanicTests
  {
    [TestCase("X",true)]
    [TestCase("Y",false)]
    public void should_handle_panic(string panicModule, bool expectedHandling)
    {
      bool handled = false;
      var bf = ImpliciXFeatureDefinition
        .DefineFeature()
        .HandlesPanic("X", () =>
        {
          handled = true;
        })
        .Create();
      bf.Execute(ModulePanic.Create(panicModule, TimeSpan.Zero));
      Check.That(handled).IsEqualTo(expectedHandling);
    }
  }
}