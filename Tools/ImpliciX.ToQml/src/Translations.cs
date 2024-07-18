using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using ImpliciX.Language.GUI;

namespace ImpliciX.ToQml;

public class Translations
{
  public Translations(Stream csvInput)
  {
    Languages = Array.Empty<string>();
    Entries = new Dictionary<string, IReadOnlyDictionary<string, string>>().AsReadOnly();
    if (csvInput == null)
      return;
    using var csvReader = new CsvReader(new StreamReader(csvInput), CultureInfo.InvariantCulture);
    csvReader.Configuration.HasHeaderRecord = true;
    var rows = csvReader
      .GetRecords<dynamic>()
      .Cast<IDictionary<string, object>>()
      .ToArray();
    var headers = rows.First().Keys.ToArray();
    var spot = headers.First();
    Languages = headers.Skip(1).ToArray();
    Entries = rows.ToDictionary(
      row => (string)row[spot],
      row => (IReadOnlyDictionary<string, string>) Languages.ToDictionary(
        language => language,
        language => (string)row[language]
        ).AsReadOnly()
      ).AsReadOnly();
  }
  public string[] Languages { get; }
  public IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Entries { get; }

  public IEnumerable<string> Check(IEnumerable<Widget> widgets)
  {
    var ws = widgets.ToArray();
    var expectedKeysForDropDowns = ws
      .Where(w => w is DropDownListWidget)
      .Cast<DropDownListWidget>()
      .Select(w => w.Value.GetType().GenericTypeArguments.First())
      .SelectMany(t => Enum.GetValues(t).Cast<Enum>())
      .Select(v => $"{v.GetType().Name}.{v}");
    var expectedKeysForTextBoxes = ws
      .Where(w => w is TextBox)
      .Cast<TextBox>()
      .Select(w => ((Node)w.Value).Urn.Value);
    return Check(expectedKeysForDropDowns.Concat(expectedKeysForTextBoxes));
  }

  public IEnumerable<string> Check(IEnumerable<Feed> feeds)
  {
    var expectedKeys = feeds
      .Where(f => f.Translate)
      .Cast<dynamic>()
      .Select(f => (string)f.Value);
    return Check(expectedKeys);
  }

  private IEnumerable<string> Check(IEnumerable<string> keys) => keys
      .Where(k => !Entries.ContainsKey(k))
      .Select(missingKey => $"Missing '{missingKey}' translation key");
}