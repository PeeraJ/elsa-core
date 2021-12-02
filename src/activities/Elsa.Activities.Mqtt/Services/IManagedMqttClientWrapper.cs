using MQTTnet;
using MQTTnet.Extensions.ManagedClient;

namespace Elsa.Activities.Mqtt.Services
{
    public interface IManagedMqttClientWrapper : IDisposable
    {
        IManagedMqttClient Client { get; }
        IManagedMqttClientOptions Options { get; }
        string Id { get; }


        Task StartAsync();
        Task StopAsync();
        void SubscribeAsync(string topic);
        Task PublishAsync(string topic, string message);
        void AddMessageHandler(Func<MqttApplicationMessage, Task> handler);
    }
}
