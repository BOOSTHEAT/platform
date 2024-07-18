using ImpliciX.Language.Core;

namespace ImpliciX.Driver.Common.CommandAggregator
{
    public class CommandAggregatorError : Error
    {
        private const string DifferentTypes = "Aggregated commands have different types";
        private const string DifferentUrns = "Aggregated commands have different Urns";

        public static CommandAggregatorError WithDifferentTypes => new CommandAggregatorError(DifferentTypes);
        public static CommandAggregatorError WithDifferentUrns => new CommandAggregatorError(DifferentUrns);

        private CommandAggregatorError(string message) : base(nameof(CommandAggregatorError), message)
        {
            // Nothing to do
        }
    }
}