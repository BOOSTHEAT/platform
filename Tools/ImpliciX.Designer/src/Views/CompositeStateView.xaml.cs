using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ImpliciX.Designer.Views
{
    public class CompositeStateView : UserControl
    {
        public CompositeStateView()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
