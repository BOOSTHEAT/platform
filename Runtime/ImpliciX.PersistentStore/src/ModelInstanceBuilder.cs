using ImpliciX.Data.Factory;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.PersistentStore
{
  public class ModelInstanceBuilder
  {
    private readonly ModelFactory _modelFactory;

    public ModelInstanceBuilder(ModelFactory modelFactory)
    {
      _modelFactory = modelFactory;
    }

    public Result<(string key,IDataModelValue value)> Create(HashValue input)
    {
      var result = _modelFactory.Create(input).Select(x => (input.Key, (IDataModelValue)x));
      if (result.IsSuccess)
        return result;
      Log.Error("Unexpected setting: {@msg}", result.Error.Message);
      return result;
    }
  }
}