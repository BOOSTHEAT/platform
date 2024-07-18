using System;

namespace ImpliciX.SharedKernel.SerialApi2.SerialPort
{
    [Flags]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4070:Non-flags enums should not be marked with \"FlagsAttribute\"",
        Justification = "Enum is flags")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S2346:Flags enumerations zero-value members should be named \"None\"",
        Justification = "Existing name is readable")]
    internal enum SerialReadWriteEvent
    {
        Error = -1,
        NoEvent = 0,
        ReadEvent = 1,
        WriteEvent = 2,
        ReadWriteEvent = ReadEvent + WriteEvent
    }
}