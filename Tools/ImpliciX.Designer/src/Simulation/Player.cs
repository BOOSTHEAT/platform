using System.Reactive.Concurrency;
using System.Threading.Tasks;

namespace ImpliciX.Designer.Simulation
{
    public delegate Task<bool> WebSocketSend(string json);

    public class Player
    {
        private readonly WebSocketSend _sender;
        private readonly IScheduler _scheduler;
        public Player(WebSocketSend sender, IScheduler scheduler=null)
        {
            
            _sender = sender;
            _scheduler = scheduler ?? Scheduler.Default;
        }

        public void Play(Scenario scenario)
        {
            foreach (var @event in scenario.Events)
            {
                _scheduler.Schedule(@event.At, () => _sender(@event.ToJson()));
            }
        }
    }
}