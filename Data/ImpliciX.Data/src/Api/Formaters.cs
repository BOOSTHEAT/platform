using System;

namespace ImpliciX.Data.Api
{
    static public class Formaters
    {
        public static string FormatTime(TimeSpan timeSpan)
        {
            return new DateTime(timeSpan.Ticks).ToString("hh:mm:ss.fff");
        }
    }
}