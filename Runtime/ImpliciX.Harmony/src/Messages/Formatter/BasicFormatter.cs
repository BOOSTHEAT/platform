using System.Text.Encodings.Web;
using System.Text.Json;

namespace ImpliciX.Harmony.Messages.Formatter
{
    public static class BasicFormatter
    {
        public static string Format<T>(this T message)
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(message, options);
        }
    }
}