using System.IO;

namespace ImpliciX.Data;

public static class StreamExtensions
{
    public static bool IsAtTheEnd(this Stream stream)
    {
        return stream.Position == stream.Length;
    }
}