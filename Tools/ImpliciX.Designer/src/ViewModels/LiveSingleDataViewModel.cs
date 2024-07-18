using System;
using ImpliciX.Data.Api;
using ImpliciX.Language.Model;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels;

public abstract class LiveSingleDataViewModel : LivePropertyViewModel
{
  private DateTime _at;

  private string _newValue;

  private string _summary;

  protected LiveSingleDataViewModel(
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
      var message = WebsocketApiV2.PropertiesMessage.WithProperties(new[] { (urn.Value, NewValue) }).ToJson();
      parent.SendToApp(message);
    };
    parent.WatchUrn(urn).Subscribe(
      change =>
      {
        Summary = change.Current.Value?.Replace(
          "|",
          "\n"
        );
      }
    );
    NewValue = Summary;
  }

  public string Summary
  {
    get =>  /*IsPercentage && !string.IsNullOrEmpty(_summary)
      ? PercentageValue(_summary)
      :*/ _summary;
    set => this.RaiseAndSetIfChanged(
      ref _summary,
      value
    );
  }

  public string NewValue
  {
    get => _newValue;
    protected set => this.RaiseAndSetIfChanged(
      ref _newValue,
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
