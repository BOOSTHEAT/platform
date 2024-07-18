using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public sealed class OnOffButtonItem : ItemBase
{
  private readonly UserSettingUrn<Presence> _targetPropertyUrn;
  public override string Title => "On/Off button";
  public override Block Display { get; }
  public override IEnumerable<Urn> PropertyInputs => new[] {_targetPropertyUrn};
  public override string UserInfoMessage => "If you put value 0 and press Enter key to validate it, then button should display its 'off' position, else button should display its 'on' position";

  public OnOffButtonItem()
  {
    _targetPropertyUrn = UserSettingUrn<Presence>.Build("control:pasteurize:auto");
    Display = OnOff(_targetPropertyUrn);
  }

  enum Presence
  {
    Yes = 1,
    No = 0
  }
}