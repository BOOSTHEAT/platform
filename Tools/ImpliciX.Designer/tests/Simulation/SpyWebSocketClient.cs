using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImpliciX.Designer.Tests.Simulation
{
    public class SpyWebSocketClient
    {
        public JsonDocument SentMessage => SentMessages.LastOrDefault();
        public List<JsonDocument> SentMessages { get; }

        public SpyWebSocketClient()
        {
            SentMessages = new List<JsonDocument>();
        }
        
#pragma warning disable 1998
        public async Task<bool> SendAsync(string json)
#pragma warning restore 1998
        {
            SentMessages.Add(JsonDocument.Parse(json));
            return true;
        } 
    }
}