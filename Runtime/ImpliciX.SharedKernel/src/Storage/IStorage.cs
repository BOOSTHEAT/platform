namespace ImpliciX.SharedKernel.Storage;

public interface IStorage
{
  public IReadFromStorage Reader { get; }
  public IWriteToStorage Writer { get; }
  public ICleanStorage Cleaner { get; }
  public IExternalBus Listener { get; }
}