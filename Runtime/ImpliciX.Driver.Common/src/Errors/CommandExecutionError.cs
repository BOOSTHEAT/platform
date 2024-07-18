using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Driver.Common.Errors
{
    public class CommandExecutionError : Error
    {
        public IDataModelValue[] ErrorProperties { get; }

        public static CommandExecutionError Create(DeviceNode deviceNode, IDataModelValue[] errorProperties)
        {
            return new CommandExecutionError($"{deviceNode.Urn} fatal error occured.", errorProperties);
        }

        private CommandExecutionError(string message, IDataModelValue[] errorProperties) : base(nameof(CommandExecutionError), message)
        {
            ErrorProperties = errorProperties;
        }
    }
}