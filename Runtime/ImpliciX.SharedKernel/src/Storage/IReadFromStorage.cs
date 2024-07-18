using System.Collections.Generic;
using ImpliciX.Data.Factory;

namespace ImpliciX.SharedKernel.Storage
{
    public interface IReadFromStorage
    {
        HashValue ReadHash(int db, string key);
        IEnumerable<HashValue> ReadAll(int db);
    }
}