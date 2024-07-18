using System;
using System.Collections.Generic;
using DynamicData;

namespace ImpliciX.DesktopServices;

public interface ISessionService : IDisposable
{
  public record Session(string Path, string Connection);
  Session Current { get; }
  IObservable<Session> Updates { get; }
  IEnumerable<Session> History { get; }
  IObservable<IEnumerable<Session>> HistoryUpdates { get; }
  
  SourceCache<ImpliciXProperty, string> Properties { get; }
}
