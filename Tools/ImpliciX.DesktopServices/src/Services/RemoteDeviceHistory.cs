using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ImpliciX.DesktopServices.Helpers;

namespace ImpliciX.DesktopServices.Services;

internal class RemoteDeviceHistory
{
  private readonly Queue<string> _connectionHistory;
  const int MaxNumberOfTargetsInHistory = 100;
  const int NumberOfSuggestionsReturnedByDefault = 3;
  internal const string PersistenceKey = "ConnectionHistory";

  public RemoteDeviceHistory()
  {
    var history = UserSettings.Read(PersistenceKey);
    _connectionHistory = history == null ? new() : JsonSerializer.Deserialize<Queue<string>>(history);
  }
  
  public void Add(string target)
  {
    if(!_connectionHistory.Contains(target))
      _connectionHistory.Enqueue(target);
    while (_connectionHistory.Count > MaxNumberOfTargetsInHistory)
      _connectionHistory.Dequeue();
    UserSettings.Set(PersistenceKey, JsonSerializer.Serialize(_connectionHistory));
  }

  public IEnumerable<string> Get(string filter)
  {
    var input = filter.Trim();
    var suggestions =
      string.IsNullOrEmpty(input)
        ? _connectionHistory.Reverse().Take(NumberOfSuggestionsReturnedByDefault)
        : _connectionHistory.Reverse().Where(s => s.Contains(input));
    foreach (var target in suggestions)
    {
      yield return target;
    }
  }

}