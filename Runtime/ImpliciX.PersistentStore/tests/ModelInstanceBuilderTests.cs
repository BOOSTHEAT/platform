using System;
using System.Collections.Generic;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Model;
using ImpliciX.SharedKernel.Storage;
using NUnit.Framework;

namespace ImpliciX.PersistentStore.Tests
{
  [TestFixture]
  public class ModelInstanceBuilderTests
  {
    [Test]
    public void should_create_model_value()
    {
      var result = _modelInstanceBuilder.Create(new HashValue("dummy:settings:my_temperature", "323.5", TimeSpan.Zero));
      Assert.That(result.IsSuccess);
      Assert.That(result.Value.key, Is.EqualTo("dummy:settings:my_temperature"));
      var modelValue = (Property<Temperature>) result.Value.value;
      Assert.That(modelValue.Urn, Is.EqualTo(dummy.settings.my_temperature));
      Assert.That(modelValue.Value, Is.EqualTo(Temperature.FromFloat(323.5f).Value));
    }
    
    [Test]
    public void should_use_backward_compatibility_when_needed()
    {
      var result = _modelInstanceBuilder.Create(new HashValue("my_old_app.temperature", "323.5", TimeSpan.Zero));
      Assert.That(result.IsSuccess);
      Assert.That(result.Value.key, Is.EqualTo("my_old_app.temperature"));
      var modelValue = (Property<Temperature>) result.Value.value;
      Assert.That(modelValue.Urn, Is.EqualTo(dummy.settings.my_temperature));
      Assert.That(modelValue.Value, Is.EqualTo(Temperature.FromFloat(323.5f).Value));
    }
    
    [SetUp]
    public void Init()
    {
      var backwardCompatibility = new Dictionary<string, Urn>
      {
        {"my_old_app.temperature", dummy.settings.my_temperature}
      };
      var modelFactory = new ModelFactory(typeof(dummy).Assembly, backwardCompatibility);
      _modelInstanceBuilder = new ModelInstanceBuilder(modelFactory);
    }
    private ModelInstanceBuilder _modelInstanceBuilder;
  }
}