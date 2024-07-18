using System.Collections.Generic;

namespace ImpliciX.DesktopServices.Services.Project;

internal record LinkerInput(
    string AppName, 
    string AppEntryPoint, 
    string Version, 
    IEnumerable<string> LinkerOptions, 
    IEnumerable<string> Binds);