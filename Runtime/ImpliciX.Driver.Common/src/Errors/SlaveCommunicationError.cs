using System;
using ImpliciX.Language.Core;
using ImpliciX.Language.Model;

namespace ImpliciX.Driver.Common.Errors
{
    public class SlaveCommunicationError : Error
    {
        public static SlaveCommunicationError Create(DeviceNode deviceNode)
        {
            return Create(deviceNode, String.Empty);
        }

        public static SlaveCommunicationError Create(DeviceNode deviceNode, string exception_message)
        {
            return new SlaveCommunicationError($"{deviceNode.Urn} an error occured while trying to communicate with the slave.Additional information : {exception_message}");
        }

        
        private SlaveCommunicationError(string message) : base(nameof(SlaveCommunicationError), message)
        {
        }
    }
}