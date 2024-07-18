using System;
using System.Threading.Tasks;

namespace ImpliciX.DesktopServices.Services;

internal interface IFileLogger : IAsyncDisposable
{
    Task WriteAsync(string text);
}