using System.Collections.Generic;
using ImpliciX.Driver.Common.Slave;
using ImpliciX.RuntimeFoundations.Events;
using ImpliciX.SharedKernel.Bricks;

namespace ImpliciX.Motors.Controllers.Tests.Board
{
    public static class ControllerExtensions
    {
        public static List<DomainEvent[]> ReadMany(this ISlaveController slaveController, int times)
        {
            var trigger = SystemTicked.Create(1000, 1);
            return slaveController.HandleMany(trigger,times);
        }
        
        public static List<DomainEvent[]> HandleMany(this ISlaveController slaveController, DomainEvent trigger, int times)
        {
            var results = new List<DomainEvent[]>();
            for (int i = 0; i < times; i++)
                results.Add(slaveController.HandleDomainEvent(trigger));
            return results;
        }
    }
}