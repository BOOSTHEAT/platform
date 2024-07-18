using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Tests;

class root : RootModelNode
{
  public root() : base(nameof(root))
  {
  }

  static root()
  {
    var root = new root();
    screen1 = new GuiNode(root, nameof(screen1));
    screen2 = new GuiNode(root, nameof(screen2));
  }

  public static GuiNode screen1 { get; }
  public static GuiNode screen2 { get; }
}

public class general : RootModelNode
{
  static general()
  {
    users = new users(new general());
  }
  public general() : base(nameof(general))
  {
  }
  
  public static users users  { get; }
}

public class user : ModelNode
{
  public user(string urnToken, ModelNode parent) : base(urnToken, parent)
  {
    language = UserSettingUrn<Language>.Build(Urn, nameof(language));
    locale = UserSettingUrn<Locale>.Build(Urn, nameof(locale));
    timezone = UserSettingUrn<TimeZone>.Build(Urn, nameof(timezone)); 
  }

  public UserSettingUrn<Language> language { get; } 
  public UserSettingUrn<Locale> locale { get; } 
  public UserSettingUrn<TimeZone> timezone{ get; } 
}

[ValueObject]
public enum Language
{
  fr = 2,
  de = 3,
  en = 0
}

[ValueObject]
public enum Locale
{
  fr__FR = 2,
  de__DE = 3,
  en__GB = 1,
  en__US = 0
}

[ValueObject]
public enum TimeZone
{
  Europe__Paris,
  Europe__London,
  America__New_York,
  Asia__Tokyo,
}

public class users : ModelNode
{
  public users(ModelNode parent) : base(nameof(users), parent)
  {
    _1 = new user("1", this);
  }

  public user _1 { get; }
} 

public class production : RootModelNode
{
  static production()
  {
    heat_pump = new heat_pump(new production());
    main_circuit = new main_circuit(new production());
  }
  public static heat_pump heat_pump { get; } 
  public static main_circuit main_circuit { get; }

  public production() : base(nameof(production))
  {
  }
}

public class main_circuit : SubSystemNode
{
    public main_circuit(ModelNode parent) : base(nameof(main_circuit), parent)
    {
      supply_pressure = new MeasureNode<Pressure>(nameof(supply_pressure), this);
    }
    public MeasureNode<Pressure> supply_pressure { get; }
}

public class heat_pump : SubSystemNode
{
  public heat_pump(ModelNode parent) : base(nameof(heat_pump), parent)
  {
    external_unit = new external_unit(this);
  }
  public external_unit external_unit { get; }
}

public class external_unit : SubSystemNode
{
  public external_unit(ModelNode parent) : base(nameof(external_unit), parent)
  {
    outdoor_temperature = new MeasureNode<Temperature>(nameof(outdoor_temperature), this);
  }
  public MeasureNode<Temperature> outdoor_temperature { get; }
}