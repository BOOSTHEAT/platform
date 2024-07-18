using ImpliciX.Language.Core;
using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.Driver.Common.CommandAggregator
{
    public static class YoungestCommandAggregator
    {
        public static Result<CommandRequested> Combine(CommandRequested c1, CommandRequested c2)
        {
            if (c1.Urn != c2.Urn)
            {
                return Result<CommandRequested>.Create(CommandAggregatorError.WithDifferentUrns);
            }

            if (c1.Arg.GetType() != c2.Arg.GetType())
            {
                return Result<CommandRequested>.Create(CommandAggregatorError.WithDifferentTypes);
            }

            return Result<CommandRequested>.Create(c1.At < c2.At ? c2 : c1);
        }
    }
}