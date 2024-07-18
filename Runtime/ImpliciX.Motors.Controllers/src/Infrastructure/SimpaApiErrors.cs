using ImpliciX.Language.Core;

namespace ImpliciX.Motors.Controllers.Infrastructure
{
    public class NotAcknowledgedError : Error
    {
        public NotAcknowledgedError() : base(nameof(NotAcknowledgedError), "Simpa frame not acknowledged") {}
    }
    public class XonError : Error
    {
        public XonError() : base(nameof(XonError), "Command not accepted") {}
    }
    
    public class UnknownResponseError : Error
    {
        public UnknownResponseError() : base(nameof(UnknownResponseError), "Unknown response") {}
    }
    
    public class WrongMotorIdResponseError : Error
    {
        public WrongMotorIdResponseError() : base(nameof(WrongMotorIdResponseError), "Wrong motor Id in response") {}
    }
    
    public class BadChecksumError : Error
    {
        public BadChecksumError() : base(nameof(BadChecksumError), "Incorrect checksum") {}
    }

    public class TimeOutError : Error
    {
        public TimeOutError() : base(nameof(TimeOutError), "The serial connection stream has been closed") {}
    }

    public class SimpaApiError : Error
    {
        public SimpaApiError(string message) : base(nameof(SimpaApiError), message) {}
    }
}