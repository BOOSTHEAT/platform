using System;
using System.Threading.Tasks;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;

namespace ImpliciX.Designer.ViewModels.LiveMenu
{
    public class ScenarioViewModel : ActionMenuViewModel<ILightConcierge>
    {
        public ScenarioViewModel(ILightConcierge concierge) : base(concierge)
        {
            Text = "Play Scenario...";
        }

        public override async void Open()
        {
            var file = await Concierge.User.OpenFile(new IUser.FileSelection
            {
                AllowMultiple = false,
                Directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            });
            if (file.Choice != IUser.ChoiceType.Ok)
                return;
            var scenario = Simulation.Scenario.Create(file.Paths[0]);
            scenario.Tap(
                async err => await ShowScenarioParsingError(err),
                s => PlayScenario(s));
        }

        private void PlayScenario(Simulation.Scenario scenario)
        {
            var player = new Simulation.Player(async (json) => await Concierge.RemoteDevice.Send(json));
            player.Play(scenario);
        }

        private async Task ShowScenarioParsingError(Error error)
        {
            await Errors.Display("Parsing error", error.Message);
        }
    }
}