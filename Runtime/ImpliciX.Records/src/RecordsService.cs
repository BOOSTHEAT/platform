using ImpliciX.Data.Factory;
using ImpliciX.Data.Records.ColdRecords;
using ImpliciX.Data.Records.HotRecords;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;
using ImpliciX.Language.Records;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Records;

internal sealed class RecordsService
{
    private readonly Records _records;
    
    private readonly IColdRecordsDb _coldRecordsDb;
    private readonly IHotRecordsDb  _hotRecordsDb;
    private readonly InstantValues  _instantValues;

    public RecordsService(
        IRecord[] records, 
        IColdRecordsDb coldRecordsDb, 
        IHotRecordsDb hotRecordsDb, 
        ModelFactory modelFactory, 
        IIdentityGenerator identityGenerator)
    {
        _records = new Records(records);
        _coldRecordsDb = coldRecordsDb;
        _hotRecordsDb = hotRecordsDb;
        var modelUrns = modelFactory.GetAllUrns().ToHashSet();
        _instantValues = new InstantValues(_records, modelUrns, identityGenerator);
    }
    public DomainEvent[] HandlePropertiesChanged(PropertiesChanged properties)
    {
        _instantValues.Update(properties);
        return Array.Empty<DomainEvent>();
    }

    public DomainEvent[] HandleCommandRequested(CommandRequested command) => Publish(command);

    private DomainEvent[] Publish(CommandRequested command)
    {
        var records = _records.GetRecords(command.Urn);
        var toPublish = new List<IDataModelValue>();
        foreach (var record in records)
        {
            _instantValues.Snapshot(record.Urn, (CommandUrn<NoArg>) command.Urn, command.At)
                .Tap(snapshot => {
                    if (record.Retention.IsSome)
                    {
                        _hotRecordsDb.Write(snapshot);
                        var outcome = _hotRecordsDb.ReadAll(record.Urn)
                            .Reverse()
                            .SelectMany((s, i) => s.HistoryOutput(i));
                        toPublish.AddRange(outcome);
                    }
                    else
                    {
                        toPublish.AddRange(snapshot.HistoryOutput(0));
                    }
                    _coldRecordsDb.Write(snapshot);
                });
        }
        return new DomainEvent[] {PropertiesChanged.Create(toPublish, command.At)};
    }

    public DomainEvent[] PublishAllRecordsHistory(TimeSpan at)
    {
        var toPublish = new List<IDataModelValue>();
        foreach (var record in _records.GetRecordsWithHistory())
        {
            var outcome = _hotRecordsDb.ReadAll(record.Urn)
                .Reverse()
                .SelectMany((s, i) => s.HistoryOutput(i));
            toPublish.AddRange(outcome);
        }
        return new DomainEvent[] {PropertiesChanged.Create(toPublish, at)};
    }
    
    public bool IsWriterCmd(CommandRequested commandRequested) => 
        _records.IsWriterCommand(commandRequested.Urn);

    
}