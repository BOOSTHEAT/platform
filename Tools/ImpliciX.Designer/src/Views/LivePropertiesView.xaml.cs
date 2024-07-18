using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ImpliciX.Designer.ViewModels;

namespace ImpliciX.Designer.Views;

public class LivePropertiesView : UserControl
{
  public LivePropertiesView()
  {
    this.InitializeComponent();
  }

  private void InitializeComponent()
  {
    AvaloniaXamlLoader.Load(this);
  }

  private void DefineClassesForRow(
    object sender,
    DataGridRowEventArgs eventArgs
  )
  {
    var row = eventArgs.Row;
    var classes = row.Classes;
    classes.Clear();
    var liveProperty = row.DataContext as LivePropertyViewModel;
    if (liveProperty == null) return;
    if (liveProperty.IsEditable) classes.Add("IsEditable");
    if (liveProperty.IsUnit) classes.Add("IsUnit");
    if (liveProperty.AsUnit) classes.Add("AsUnit");
    foreach (var livePropertyClass in liveProperty.Classes) classes.Add(livePropertyClass);
  }

  private void InputElement_OnKeyDown(
    object sender,
    KeyEventArgs e
  )
  {
    if (Key.Enter.Equals(e.Key) || Key.Return.Equals(e.Key))
      ((LivePropertyViewModel)((StackPanel)sender).DataContext)?.SetNewValue.Invoke();
  }
}
