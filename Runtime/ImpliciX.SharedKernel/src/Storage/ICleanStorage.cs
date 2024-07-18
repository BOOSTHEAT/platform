using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Storage
{
    public interface ICleanStorage
    {
        Result<Unit> FlushDb(int db);
    }
}