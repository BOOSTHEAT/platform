using ImpliciX.Language.Model;
using ImpliciX.Language.Records;
using ImpliciX.SharedKernel.Collections;

namespace ImpliciX.Records;

public class Records
{
    private readonly IRecord[] _records;
    
    private Dictionary<CommandUrn<NoArg>, List<IRecord>> RecordsCommandMap { get; }
    public HashSet<Urn> FormUrns { get; }
    private Dictionary<CommandUrn<NoArg>, Urn> FormsUrnByCommandMap { get; }
  
    public Records(IRecord[] records)
    {
        _records = ValidateRecords(records);
 
         FormUrns = _records.SelectMany(r=>r.Writers)
             .Select(w=>w.FormUrn)
             .ToHashSet();

        FormsUrnByCommandMap = _records
            .SelectMany(r => r.Writers)
            .Aggregate(new Dictionary<CommandUrn<NoArg>, Urn>(), (acc, w) =>
            {
                acc[w.CommandUrn] = w.FormUrn;
                return acc;
            });
            
        RecordsCommandMap = _records.Aggregate(new Dictionary<CommandUrn<NoArg>, List<IRecord>>(), (acc, r) =>
        {
            foreach (var writer in r.Writers)
            {
                if (!acc.ContainsKey(writer.CommandUrn))
                    acc[writer.CommandUrn] = new List<IRecord>();
                acc[writer.CommandUrn].Add(r);
            }
            return acc;
        });
    }
    
    public IRecord[] GetRecordsWithHistory() => _records.Where(r => r.Retention.IsSome).ToArray();
    
    public IRecord[] GetRecords(Urn commandUrn)
        => RecordsCommandMap.TryGetValue((CommandUrn<NoArg>)commandUrn, out var records) 
            ? records.ToArray() 
            : Array.Empty<IRecord>();

    public Urn? FormUrnForCommand(CommandUrn<NoArg> commandUrn) => 
        FormsUrnByCommandMap.GetValueOrDefault(commandUrn);

    private static IRecord[] ValidateRecords(IRecord[] records)
    {
        if (records == null) throw new ArgumentNullException(nameof(records));
        var errorMessage = "";
        var recordsAreUnique = records.DistinctBy(r => r.Urn).Count() == records.Length;
        if (!recordsAreUnique)
            errorMessage += "The records must have unique identifiers.";
        records.ForEach(record =>
        {
            var writersInRecordAreUnique = record.Writers.DistinctBy(w => w.CommandUrn).Count() == record.Writers.Count;
            if (!writersInRecordAreUnique)
                errorMessage += $"Record {record.Urn} contains duplicated writers with the same CommandUrn";
        });

        if (errorMessage.Length > 0)
            throw new InvalidOperationException(errorMessage);

        return records;
    }

    public bool IsWriterCommand(Urn commandRequestedUrn) =>
        commandRequestedUrn is CommandUrn<NoArg> candidate 
        && RecordsCommandMap.ContainsKey(candidate);
}