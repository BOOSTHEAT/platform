using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Driver.Common.Errors
{
    public class ReadProtocolError : Error
    {
        public static ReadProtocolError Create(DeviceNode deviceNode)
        {
            return Create(deviceNode, string.Empty);
        }
        
        public static ReadProtocolError Create(DeviceNode deviceNode, string exception_message)
        {
            return new ReadProtocolError($"{deviceNode.Urn} tried to read registers and something went wrong. Additional information {exception_message}" );
        }

        private ReadProtocolError(string message) : base(nameof(ReadProtocolError), message)
        {
        }
    }
}