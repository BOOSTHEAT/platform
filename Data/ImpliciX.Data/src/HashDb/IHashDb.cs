using System.Collections.Generic;
using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;

namespace ImpliciX.Data.HashDb;

public interface IHashDb
{
  Result<Unit> DeleteAll();
  Result<Unit> Write(HashValue hash);
  Result<HashValue> Read(string key);
  Result<IEnumerable<HashValue>> ReadAll();
}