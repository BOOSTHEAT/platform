namespace ImpliciX.RTUModbus.Controllers.BrahmaBoard
{
    public enum State : ushort
    {
        DisabledAvailable =    0b0000000000000001,
        DisabledNotAvailable = 0b0000000000000010,
        EnabledNotAvailable =  0b0000000000000100,
        
        EnabledAvailable =     0b0000000000001000,
        StandBy =              0b0000000000010000,
        Supplying =            0b0000000000100000,
        Supplied =             0b0000000001000000,
        Resetting =            0b0000000010000000,
        ResetPerformed =       0b0000000100000000,
        CheckReadiness =       0b0000001000000000,
        Faulted =              0b0000010000000000,
        Ready =                0b0000100000000000,
        Running =              0b0001000000000000,
        WaitingIgnition =      0b0010000000000000,
        Igniting =             0b0100000000000000,
        Ignited =              0b1000000000000000,

        EnabledAvailableStatesMask =
            EnabledAvailable |
            StandBy |
            Supplying |
            Supplied |
            Resetting |
            ResetPerformed |
            CheckReadiness |
            Faulted |
            Ready |
            Running |
            WaitingIgnition |
            Igniting |
            Ignited
    }
}
