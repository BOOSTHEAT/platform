using Avalonia.Controls;

namespace dialogs_demo;

public partial class MainWindow : Window
{
  public MainWindow()
  {
    InitializeComponent();
    var data = new Data();
    data.Message.RegisterOn(this);
    DataContext = data;
  }
}