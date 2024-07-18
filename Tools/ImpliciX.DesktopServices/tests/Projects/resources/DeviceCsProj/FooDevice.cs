using System.Reflection;
using ImpliciX.Language;
using ImpliciX.Language.Model;

namespace ImpliciX.FooDevice;

public class Main : ApplicationDefinition
{
  public Main()
  {
    AppName = "Foo";
    AppSettingsFile = "appsettings.json";

    DataModelDefinition = new DataModelDefinition
    {
      Assembly = Assembly.GetExecutingAssembly()
    };

    ModuleDefinitions = new object[]
    {
    };
  }
}