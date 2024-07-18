using System;
using System.Collections.ObjectModel;
using System.Linq;
using ImpliciX.Data.Api;
using ImpliciX.Language.Model;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public class LiveFunctionDefinitionViewModel : LivePropertyViewModel
{
  private DateTime _at;

  private ObservableCollection<Summary> _summaries = new ();

  private string _summary;

  public LiveFunctionDefinitionViewModel(
    Urn urn,
    LivePropertiesViewModel parent,
    bool inModel
  ) : base(
    urn,
    parent,
    inModel
  )
  {
    SetNewValue = () =>
    {
      var properties = Summaries.Select(c => c.GetProperty()).Aggregate(
        (
          s1,
          s2
        ) => s1 + "|" + s2
      );
      foreach (var summary in _summaries) summary.ClearNewValue();
      var message = WebsocketApiV2.PropertiesMessage.WithProperties(new[] { (urn.Value, properties) }).ToJson();
      parent.SendToApp(message);
    };

    parent.WatchUrn(urn).Subscribe(
      change =>
      {
        Summary = change.Current.Value?.Replace(
          "|",
          "\n"
        );

        foreach (var val in change.Current.Value?.Split("|")!)
        {
          var property = new SummaryProperty(val);
          var summary = _summaries.FirstOrDefault(c => c.Name == property.Name);
          if (summary == null)
            _summaries?.Add(
              new Summary(
                urn,
                property
              )
            );
          else
            summary.Value = property.Value;
        }
      }
    );
  }

  public string Summary
  {
    get => _summary;
    private set => this.RaiseAndSetIfChanged(
      ref _summary,
      value
    );
  }

  public ObservableCollection<Summary> Summaries
  {
    get => _summaries;
    private set => this.RaiseAndSetIfChanged(
      ref _summaries,
      value
    );
  }


  public DateTime At
  {
    get => _at;
    private set => this.RaiseAndSetIfChanged(
      ref _at,
      value
    );
  }
}

public class SummaryProperty
{
  public SummaryProperty(
    string property
  )
  {
    var splitProperty = property.Split(":");
    switch (splitProperty.Length)
    {
      case 2:
        Name = splitProperty[0];
        Value = splitProperty[1];
        break;
      default:
        throw new ArgumentException($"{property} : is in wrong format");
    }
  }

  public string Value { get; set; }

  public string Name { get; set; }
}

public class Summary : ReactiveObject
{
  private readonly Urn _urn;
  private string _name;

  private string _newValue;

  private string _value;

  public Summary(
    Urn urn,
    SummaryProperty property
  )
  {
    _urn = urn;
    _name = property.Name;
    _value = property.Value;
  }

  public string Name
  {
    get => _name;
    set => this.RaiseAndSetIfChanged(
      ref _name,
      value
    );
  }

  public string Value
  {
    get => _value;
    set => this.RaiseAndSetIfChanged(
      ref _value,
      value
    );
  }

  public string NewValue
  {
    get => _newValue;
    set => this.RaiseAndSetIfChanged(
      ref _newValue,
      value
    );
  }

  public string GetProperty()
  {
    return string.IsNullOrWhiteSpace(NewValue)
      ? $"{Name}:{Value}"
      : $"{Name}:{NewValue}";
  }

  public void ClearNewValue()
  {
    NewValue = null;
  }
}
