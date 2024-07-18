using ImpliciX.Language.Model;

namespace ImpliciX.Api.WebSocket.Tests;

public class dummy : RootModelNode
{
  public dummy(string urnToken) : base(urnToken)
  {
  }
  public static PropertyUrn<SoftwareVersion> version =>
    PropertyUrn<SoftwareVersion>.Build(nameof(dummy), nameof(version));
  public static PropertyUrn<Literal> environment =>
    PropertyUrn<Literal>.Build(nameof(dummy), nameof(environment));

  
  public static CommandUrn<Literal> dummy_command =>
    CommandUrn<Literal>.Build(nameof(dummy), nameof(dummy_command));

  public static PropertyUrn<PowerSupply> dummy_property1 =>
    PropertyUrn<PowerSupply>.Build(nameof(dummy), nameof(dummy_property1));

  public static PropertyUrn<Percentage> dummy_property2 =>
    PropertyUrn<Percentage>.Build(nameof(dummy), nameof(dummy_property2));

  public static UserSettingUrn<PowerSupply> dummy_configuration1 =>
    UserSettingUrn<PowerSupply>.Build(nameof(dummy), nameof(dummy_configuration1));

  public static UserSettingUrn<Percentage> dummy_configuration2 =>
    UserSettingUrn<Percentage>.Build(nameof(dummy), nameof(dummy_configuration2));

  public static UserSettingUrn<Presence> dummy_configuration3 =>
    UserSettingUrn<Presence>.Build(nameof(dummy), nameof(dummy_configuration3));

  public static VersionSettingUrn<FunctionDefinition> dummy_func =>
    VersionSettingUrn<FunctionDefinition>.Build(nameof(dummy), nameof(dummy_func));
}