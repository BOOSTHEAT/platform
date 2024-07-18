using System;
using ImpliciX.Data.Api;
using ImpliciX.Data.Factory;
using ImpliciX.DesktopServices;
using ImpliciX.Language.Core;
using ReactiveUI;

namespace ImpliciX.Designer.ViewModels
{
    public class CommandsViewModel : DockableViewModel
    {
        public CommandsViewModel(ILightConcierge concierge)
        {
            _concierge = concierge;
            _concierge.Applications.Devices
                .Subscribe(odd => ModelFactory = odd.Match(
                    () => null,
                    dd => dd.ModelFactory
                    ));
            Title = "Commands";
        }

        public void OnSendingCommand()
        {
            var tokens = CommandBox.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var result = tokens[0] switch
            {
                "set" => CreatePropertyMessage(tokens, ModelFactory),
                _ => CreateCommandMessage(tokens, ModelFactory)
            };
            if (result.IsError)
            {
                CommandHistory = $"{CommandBox}({result.Error.Message}){Environment.NewLine}{CommandHistory}";
                return;
            }
            var msg = result.Value;
            _concierge.RemoteDevice?.Send(msg);
            CommandHistory = $"{CommandBox}{Environment.NewLine}{CommandHistory}";
            CommandBox = string.Empty;
        }

        private static Result<string> CreateCommandMessage(string[] tokens, ModelFactory factory)
        {
            var urn = tokens[0];
            var arg = tokens.Length > 1
                ? tokens[1]
                : string.Empty;
            return
                from checkUrn in factory.CreateWithLog(urn, arg, DateTime.Now.TimeOfDay)
                select WebsocketApiV2.CommandMessage.WithParameter(urn, arg).ToJson();
        }

        private static Result<string> CreatePropertyMessage(string[] tokens, ModelFactory factory)
        {
            if (tokens.Length != 3)
                return Result<string>.Create(new Error("Syntax", "set <urn> <value>"));
            var urn = tokens[1];
            var arg = tokens[2];
            return
                from checkUrn in factory.CreateWithLog(urn, arg, DateTime.Now.TimeOfDay)
                select WebsocketApiV2.PropertiesMessage.WithProperties(new[] { (urn, arg) }).ToJson();
        }

        public string CommandHistory
        {
            get => _commandHistory;
            set => this.RaiseAndSetIfChanged(ref _commandHistory, value);
        }

        private string _commandHistory;

        public string CommandBox
        {
            get => _commandBox;
            set
            {
                SendButtonVisible = !string.IsNullOrWhiteSpace(value);
                this.RaiseAndSetIfChanged(ref _commandBox, value);
            }
        }

        private string _commandBox;

        public bool SendButtonVisible
        {
            get => _sendButtonVisible;
            set => this.RaiseAndSetIfChanged(ref _sendButtonVisible, value);
        }

        private bool _sendButtonVisible;

        public ModelFactory ModelFactory
        {
            get => _modelFactory;
            set => this.RaiseAndSetIfChanged(ref _modelFactory, value);
        }
        private ModelFactory _modelFactory;

        private ILightConcierge _concierge;
    }
}