using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Extensions.ManagedClient;

namespace Elsa.Activities.Mqtt.Services
{
    public class ManagedMqttClientWrapper : IManagedMqttClientWrapper
    {
        public IManagedMqttClient Client { get; }
        public IManagedMqttClientOptions Options { get; }
        public string Id { get; }

        public ManagedMqttClientWrapper(IManagedMqttClient client, IManagedMqttClientOptions options, string id)
        {
            Client = client;
            Options = options;
            Id = id;
        }

        public async Task StartAsync()
        {
            try
            {
                await Client.StartAsync(Options);
                var ic = Client.IsConnected;
            }
            catch (Exception ex)
            {
                var i = ex.InnerException;
            }
        }

        public async Task StopAsync()
        {
            await Client.StopAsync();
        }

        public void SubscribeAsync(string topic)
        {
            Client.UseConnectedHandler(async x =>
            {
                await Client.SubscribeAsync(topic);
            });
        }

        public async Task PublishAsync(string topic, string message)
        {
            try
            {
                await Client.PublishAsync(topic, message);
            }
            catch (Exception ex)
            {
                var t = ex.InnerException;
            }
        }

        public void AddMessageHandler(Func<MqttApplicationMessage, Task> handler)
        {
            Client.UseApplicationMessageReceivedHandler(async x =>
            {
                await handler(x.ApplicationMessage);
            });
        }

        public void Dispose()
        {
            Client.Dispose();
        }
    }
}
