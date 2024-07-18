using ImpliciX.Data.Factory;
using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.Data.Records.HotRecords;
using ImpliciX.Language.Model;
using ImpliciX.Language.Records;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;
using ImpliciX.TestsCommon;
using Moq;
using NFluent;

namespace ImpliciX.Records.Tests.Helpers;

internal sealed class Context
{
    private readonly RecordsService _recordsService;

    public static Context Create(IRecord[] records, IColdRecordsDb? coldRecordsDb = null)
    {
        var folderPath = Path.Combine("/tmp/", "records");
        if (Directory.Exists(folderPath))
        {
            Directory.Delete(folderPath, true);
        }
        var hotRecordsDb =  new HotRecordsDb(records, folderPath, "records");

        var modelFactory = new ModelFactory(typeof(model).Assembly);
        var recordsService = new RecordsService(records, 
            coldRecordsDb ?? Mock.Of<IColdRecordsDb>(), 
            hotRecordsDb, 
            modelFactory,
            new SequentialIdGenerator());
        var context = new Context(recordsService);
        return context;
    }

    private Context(RecordsService recordsService)
    {
        _recordsService = recordsService ?? throw new ArgumentNullException(nameof(recordsService));
    }

    public DomainEvent[] PublishAllRecordsHistory(TimeSpan at)
    {
        return _recordsService.PublishAllRecordsHistory(at);
    }
        

    public Context Publish(Urn urn, float newValue, TimeSpan at)
    {
        _recordsService.HandlePropertiesChanged(PropertiesChanged.Create(new[] {CreateFloatProperty(urn, newValue, at)}, at));
        return this;
    }

    public Context Publish(IDataModelValue[] mvs)
    {
        _recordsService.HandlePropertiesChanged(PropertiesChanged.Create(mvs, mvs[0].At));
        return this;
    }

    public void Publish<T>(Urn urn, T state, TimeSpan at) where T : Enum
        => _recordsService.HandlePropertiesChanged(CreateStateChanged(PropertyUrn<T>.Build(urn), state, at));

    private static PropertiesChanged CreateStateChanged<T>(PropertyUrn<T> urn, T state, TimeSpan at) where T : Enum
        => PropertiesChanged.Create(new[] {Property<T>.Create(urn, state, at)}, at);

    internal static IDataModelValue CreateFloatProperty(string urn, float value, TimeSpan at)
        => Property<FloatValue>.Create(PropertyUrn<FloatValue>.Build(urn), new FloatValue(value), at);

    internal static IDataModelValue CreateEnumProperty<T>(string urn, T value, TimeSpan at) where T : Enum
        => Property<T>.Create(PropertyUrn<T>.Build(urn), value, at);

    public IDataModelValue[] ExecuteCommand(CommandUrn<NoArg> commandUrn, TimeSpan at)
    {
        var events = _recordsService.HandleCommandRequested(CommandRequested.Create(commandUrn, new NoArg(), at));
        return events
            .OfType<PropertiesChanged>()
            .SelectMany(o => o.ModelValues)
            .ToArray();
    }

    #region Helpers

    public static void CheckModelValue<T>(IDataModelValue modelValue, Urn urnExpected, T valueExpected, TimeSpan atExpected)
    {
        Check.That(modelValue.Urn).IsEqualTo(urnExpected);
        Check.That(modelValue.ModelValue()).IsEqualTo(valueExpected);
        Check.That(modelValue.At).IsEqualTo(atExpected);
    }

    public static Urn CreateOutputUrn(int index, string urnToken, string? rootTargetUrn = null)
        => Urn.BuildUrn(rootTargetUrn ?? model.the_alarms.Urn, index.ToString(), urnToken);

    public static FloatValue F(float value) => FloatValue.FromFloat(value).Value;
    
    #endregion
}