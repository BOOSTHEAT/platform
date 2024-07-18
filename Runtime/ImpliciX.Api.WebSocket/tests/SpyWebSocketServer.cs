using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

#pragma warning disable 1998

namespace ImpliciX.Api.WebSocket.Tests
{
  public class SpyWebSocketServer
  {
    public IReadOnlyCollection<JsonDocument> SentMessages => _sentMessages;
    private readonly List<JsonDocument> _sentMessages;
    public JsonDocument LatestSentMessage { get; set; }

    public SpyWebSocketServer()
    {
      _sentMessages = new List<JsonDocument>();
    }

    public async Task<bool> SendAsync(Guid clientId, string json)
    {
      LatestSentMessage = JsonDocument.Parse(json);
      _sentMessages.Add(LatestSentMessage);
      return true;
    }
  }
}