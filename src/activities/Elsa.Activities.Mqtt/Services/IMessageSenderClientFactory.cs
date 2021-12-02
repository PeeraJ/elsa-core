using MQTTnet.Extensions.ManagedClient;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IMessageSenderClientFactory
    {
        Task<IManagedMqttClientWrapper> GetSenderAsync(string topic, CancellationToken cancellationToken = default);
    }
}