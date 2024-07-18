using ImpliciX.RuntimeFoundations.Events;

namespace ImpliciX.RTUModbus.Controllers.Tests.Doubles
{
    public static class TestEnv
    {
        public static CommunicationDetails Healthy_CommunicationDetails = new CommunicationDetails(1,0);
        public static CommunicationDetails Error_CommunicationDetails = new CommunicationDetails(0,1);
        public static CommunicationDetails Zero_CommunicationDetails = new CommunicationDetails(0,0);
    }
}