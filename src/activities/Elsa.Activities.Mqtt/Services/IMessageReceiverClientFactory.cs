using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMessageReceiverClientFactory
    {
        Task<IManagedMqttClientWrapper> GetReceiverAsync(string topic, CancellationToken cancellationToken = default);
        Task DisposeReceiverAsync(IManagedMqttClientWrapper receiverClient, CancellationToken cancellationToken = default);
    }
}