using System;
using System.Collections.Generic;
using System.Linq;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Storage;

namespace ImpliciX.PersistentStore
{
  public delegate void Publish(params PropertiesChanged[] propertyChanged);

  public delegate Result<(string key, IDataModelValue value)> CreateModelObject(HashValue input);

  public delegate TimeSpan Clock();

  public class PersistentStorePublisher : IDisposable
  {
    private readonly Publish _publish;
    private readonly CreateModelObject _factory;
    private readonly IExternalBus _externalBus;
    private readonly Clock _clockAdapter;
    private readonly IReadFromStorage _reader;
    private readonly Type _propertyKind;
    private readonly int _db;

    public PersistentStorePublisher(Type propertyKind, int db, Publish publish, IReadFromStorage reader,
      CreateModelObject createObject, IExternalBus externalBus, Clock clockAdapter)
    {
      _propertyKind = propertyKind;
      _db = db;
      _publish = publish;
      _reader = reader;
      _externalBus = externalBus;
      _factory = createObject;
      _clockAdapter = clockAdapter;
      SendFullConfig();
    }

    public void Run()
    {
      _externalBus.SubscribeAllKeysModification(_db, key =>
      {
        var hv = _reader.ReadHash(_db, key);
        hv.SetAtField(_clockAdapter());
        var factoryResult = _factory(hv);
        if (factoryResult.IsSuccess)
          _publish(PropertiesChanged.Create(new List<IDataModelValue>() { factoryResult.Value.value },
            _clockAdapter()));
        else
          Log.Error($"Configurator: Read error -- {factoryResult.Error.Message}");
      });
    }

    private void SendFullConfig()
    {
      var config = _reader.ReadAll(_db).ToList();
      config.ForEach(c => c.SetAtField(_clockAdapter()));
      var factoryResults = config.Select(hv => _factory(hv)).ToList();

      var configValues = (
        from result in factoryResults
        where result.IsSuccess
        group result by result.Value.value.Urn into r
        let preferred = r.Select(x => x.Value).Where(c => c.key == r.Key).Select(x => x.value).ToArray()
        select preferred.Any() ? preferred.First() : r.First().Value.value
      ).ToList();

      var unexpectedSettings = (from cv in configValues
        let settingKind = cv.Urn.GetType().GetGenericTypeDefinition()
        where settingKind != _propertyKind
        select cv.Urn).Cast<object>().ToArray();
      if (unexpectedSettings.Any())
      {
        var expectedKind = _propertyKind.Name.Substring(0, _propertyKind.Name.Length - 2);
        throw new Exception($"Unexpected settings (should be {expectedKind}): {string.Join(",", unexpectedSettings)}");
      }

      _publish(PropertiesChanged.Create(configValues, _clockAdapter()));

      factoryResults.Where(result => result.IsError)
        .Select(result => result.Error.Message)
        .ToList()
        .ForEach(msg => Log.Error("Configurator: Read error {@msg}", msg));
    }

    public void Dispose()
    {
    }
  }
}