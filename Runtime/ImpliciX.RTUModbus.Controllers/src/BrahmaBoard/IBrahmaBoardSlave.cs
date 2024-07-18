using ImpliciX.Driver.Common;
using ImpliciX.Language.Model;

namespace ImpliciX.RTUModbus.Controllers.BrahmaBoard
{
    public interface IBrahmaBoardSlave : IBoardSlave
    {
        BurnerNode GenericBurner { get; }
    }
}