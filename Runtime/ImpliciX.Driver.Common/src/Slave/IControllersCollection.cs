using System.Collections.Generic;

namespace ImpliciX.Driver.Common.Slave
{
    public interface IControllersCollection : ISlaveController, IEnumerable<ISlaveController>
    {
    }
}