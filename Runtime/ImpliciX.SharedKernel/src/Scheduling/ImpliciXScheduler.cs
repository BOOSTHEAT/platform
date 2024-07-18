using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace ImpliciX.SharedKernel.Scheduling
{
    public abstract class ImpliciXScheduler : IHostedService
    {
        public abstract Task StartAsync(CancellationToken cancellationToken);
        public abstract Task StopAsync(CancellationToken cancellationToken);
    }
}