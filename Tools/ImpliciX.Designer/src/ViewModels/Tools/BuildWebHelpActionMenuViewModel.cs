using System;
using Avalonia.Controls;
using ImpliciX.DesktopServices;
using JetBrains.Annotations;

namespace ImpliciX.Designer.ViewModels.Tools
{
    internal sealed class BuildWebHelpActionMenuViewModel : ActionMenuViewModel<IConcierge>
    {
        [NotNull] private readonly Func<Window> _getWindowOwner;

        public BuildWebHelpActionMenuViewModel([NotNull] Func<Window> getWindowOwner, [NotNull] IConcierge concierge)
            : base(concierge)
        {
            _getWindowOwner = getWindowOwner ?? throw new ArgumentNullException(nameof(getWindowOwner));
            Text = "Build WebHelp...";
        }

        public override async void Open()
            => await new BuildWebHelpViewModel(Concierge).ShowDialogAsync(_getWindowOwner());
    }
}