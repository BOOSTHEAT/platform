namespace ImpliciX.Alarms
{
    public class AlarmSettings
    {
        public ConsecutiveErrors ConsecutiveSlaveCommunicationErrorsBeforeFailure { get; set; }
        
        public class ConsecutiveErrors
        {
            public int Default { get; set; }
            public SlaveErrors[] Override { get; set; }
            public class SlaveErrors
            {
                public string Slave { get; set; }
                public int Value { get; set; }
            }

        }

    }
}
