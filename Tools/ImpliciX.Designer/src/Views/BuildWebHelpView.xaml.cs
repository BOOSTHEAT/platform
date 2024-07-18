using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views
{
    public class BuildWebHelpView : Window
    {
        public BuildWebHelpView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}