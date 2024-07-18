using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views
{
    public class CommandsView : UserControl
    {
        public CommandsView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
