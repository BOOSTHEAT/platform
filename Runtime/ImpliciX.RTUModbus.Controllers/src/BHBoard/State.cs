namespace ImpliciX.RTUModbus.Controllers.BHBoard
{
    public enum State
    {
        Disabled, //on fait rien
        Working, // * état par défaut
        Initializing,
        Regulation,
        Updating,
        UpdateInitializing,
        UpdateStarting,
        WaitingUploadReady,
        Uploading,
        UploadCompleted
        //...
    }
}