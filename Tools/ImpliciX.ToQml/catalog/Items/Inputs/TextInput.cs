using System.Drawing;
using ImpliciX.Language.GUI;
using ImpliciX.Language.Model;

namespace ImpliciX.ToQml.Catalog.Items;

public class TextInput : ItemBase
{
    public override string Title => "Text Input";

    public TextInput()
    {
        _userText1 = PropertyUrn<Literal>.Build(Root.Urn.Value, "user_text_1");
        _userText2 = PropertyUrn<Literal>.Build(Root.Urn.Value, "user_text_2");
        _localeUrn = UserSettingUrn<DropDownLocale>.Build("general", "locale");
        _timezoneUrn = UserSettingUrn<DropDownTimeZone>.Build("general", "timezone");
    }

    public override Block Display => Column.Layout(
        Input(_userText1).Width(200),
        Input(_userText2).Width(400).With(Font.ExtraBold.Size(32).Color(Color.Blue)),
        DropDownList(_localeUrn).Width(200),
        DropDownList(_timezoneUrn).Width(200)

    );

    public override IEnumerable<Urn> PropertyInputs => new Urn[] { _userText1, _userText2 };

    public override IDictionary<Urn, string> DefaultValues => new Dictionary<Urn, string>
    {
        [_userText1] = "'Hello World'",
        [_userText2] = "'This is Big Blue'",
    };

    private readonly PropertyUrn<Literal> _userText1;
    private readonly PropertyUrn<Literal> _userText2;
    private readonly UserSettingUrn<DropDownLocale> _localeUrn;
    private readonly UserSettingUrn<DropDownTimeZone> _timezoneUrn;
    
    enum DropDownLocale
    {
        en_GB = Locale.en_GB,
        fr_FR = Locale.fr_FR,
        de_DE = Locale.de_DE,
        fr_BE = Locale.fr_BE,
    }
    
    enum DropDownTimeZone
    {
        Europe__Paris = Language.Model.TimeZone.Europe__Paris,
        America__New_York = Language.Model.TimeZone.America__New_York,
        Asia__Bangkok = Language.Model.TimeZone.Asia__Bangkok
    }
}