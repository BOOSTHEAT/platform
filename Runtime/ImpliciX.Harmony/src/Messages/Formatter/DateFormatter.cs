using System;

namespace ImpliciX.Harmony.Messages.Formatter
{
    public static class DateFormatter
    {
        public static string Format(this TimeSpan timeSpan) =>
            new DateTime(timeSpan.Ticks, DateTimeKind.Utc).Format();

        public static string Format(this DateTime dateTime) =>
            dateTime.ToString(DATE_FORMAT);

        private const string DATE_FORMAT = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.ffffffzzz";
    }
}