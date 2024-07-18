using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;

namespace ImpliciX.SharedKernel.Storage
{
    public interface IWriteToStorage
    {
        Result<Unit> WriteHash(int db, HashValue value);
    }
}