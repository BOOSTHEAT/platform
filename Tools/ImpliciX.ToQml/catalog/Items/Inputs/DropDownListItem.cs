using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items.Inputs;

public sealed class DropDownListItem : ItemBase
{
  private readonly UserSettingUrn<DropDownPresence> _targetPropertyUrn;
  public override string Title => "Drop Down list";
  public override Block Display { get; }
  public override IEnumerable<Urn> PropertyInputs => new Urn[] {_targetPropertyUrn};
  public override string UserInfoMessage => $"Input one the the following values in {_targetPropertyUrn.Value} to change the drop down value: {string.Join(',',Enum.GetValues(typeof(DropDownPresence)).Cast<object>().Select(v => (int)v))}";
  public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
  {
    [_targetPropertyUrn] = "44",
  };
  public DropDownListItem()
  {
    _targetPropertyUrn = UserSettingUrn<DropDownPresence>.Build("general","example","dropdownlist");
    Display = Column.Layout(
      DropDownList(_targetPropertyUrn),
        DropDownList(_targetPropertyUrn).Width(500)
      );
  }
  
  enum DropDownPresence
  {
    Yo = 23,
    Nein = 44,
    Benleu = 58,
  }
}