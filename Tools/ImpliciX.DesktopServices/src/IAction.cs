using ImpliciX.Language.Core;

namespace ImpliciX.DesktopServices;

public interface IAction
{
    Result<Unit> Execute();
}