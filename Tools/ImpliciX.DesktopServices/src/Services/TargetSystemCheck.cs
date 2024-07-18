using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services;

internal class TargetSystemCheck : ITargetSystemCapability
{
  private readonly IBaseConcierge _concierge;
  private readonly IConsoleOutputSlice _consoleOutputSlice;
  private readonly ITargetSystem _target;

  internal TargetSystemCheck(IBaseConcierge concierge, IConsoleOutputSlice consoleOutputSlice, ITargetSystem target)
  {
    _concierge = concierge;
    _consoleOutputSlice = consoleOutputSlice;
    _target = target;
  }

  public bool IsAvailable => true;

  public ITargetSystemCapability.IExecution Execute(params string[] args) => new Execution(this, args);

  class Execution : ITargetSystemCapability.IExecution
  {
    private readonly TargetSystemCheck _targetSystemCheck;

    public Execution(TargetSystemCheck targetSystemCheck, string[] args = null)
    {
      _targetSystemCheck = targetSystemCheck;
    }

    public Task AndWriteResultToConsole()
    {
      throw new NotSupportedException();
    }

    public async Task AndSaveTo(string destination)
    {
      _targetSystemCheck._concierge.Console.WriteLine("System check started.");
      await _targetSystemCheck._consoleOutputSlice.DumpInto(Path.Combine(destination, "console.txt"));
      if (_targetSystemCheck._target.SystemJournalBackup.IsAvailable)
      {
        await _targetSystemCheck._target.SystemJournalBackup.Execute()
          .AndSaveTo(Path.Combine(destination, "log.txt.gz"));
      }

      if (_targetSystemCheck._target.ImplicixVarLibBackup.IsAvailable)
      {
        await _targetSystemCheck._target.ImplicixVarLibBackup.Execute()
          .AndSaveTo(Path.Combine(destination, "var_lib.gz"));
      }

      _targetSystemCheck._concierge.Console.WriteLine($"System check complete and saved in {destination}");
    }

    public IAsyncEnumerable<(int count, int length, string name, string checksum)> AndSaveManyTo(string destination)
    {
      throw new NotSupportedException();
    }
  }
}
