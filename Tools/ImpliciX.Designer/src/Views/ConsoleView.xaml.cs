using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData.Binding;

namespace ImpliciX.Designer.Views
{
  public class ConsoleView : UserControl
  {
    public ConsoleView()
    {
      this.InitializeComponent();
      var scrollViewer = this.FindControl<ScrollViewer>("ScrollViewer");
      this.FindControl<ItemsControl>("Items")
        .WhenPropertyChanged(x => x.ItemCount)
        .Subscribe(x =>
        {
          Dispatcher.UIThread.Invoke(scrollViewer!.ScrollToEnd);
        });
    }

    private void InitializeComponent()
    {
      AvaloniaXamlLoader.Load(this);
    }
  }
}