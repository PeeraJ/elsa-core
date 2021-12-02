using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttTopicsStarter//tODO: name?
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
    }
}