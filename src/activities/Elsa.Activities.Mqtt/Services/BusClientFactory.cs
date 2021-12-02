using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Formatter;
using Storage.Net.Messaging;

namespace Elsa.Activities.Mqtt.Services
{
    public class BusClientFactory : IMessageReceiverClientFactory, IMessageSenderClientFactory
    {
        private readonly IManagedMqttClientOptions _optionsS;
        private readonly IManagedMqttClientOptions _optionsR;
        // private readonly ManagementClient _managementClient;
        private readonly IDictionary<string, IManagedMqttClientWrapper> _senders = new Dictionary<string, IManagedMqttClientWrapper>();
        private readonly IDictionary<string, IManagedMqttClientWrapper> _receivers = new Dictionary<string, IManagedMqttClientWrapper>();
        private IMqttFactory _factory;
        private readonly SemaphoreSlim _semaphore = new(1);

        public BusClientFactory()
        {
            //_options = options;
            // _managementClient = managementClient;
            _factory = new MqttFactory();

            //var tlsOptions = new MqttClientTlsOptions
            //{
            //    UseTls = false, IgnoreCertificateChainErrors = true, IgnoreCertificateRevocationErrors = true, AllowUntrustedCertificates = true
            //};

            //var clientOptions = new MqttClientOptions
            //{
            //    ClientId = "Elsa_" + Guid.NewGuid().ToString(),
            //    ProtocolVersion = MqttProtocolVersion.V311,
            //    ChannelOptions = new MqttClientTcpOptions
            //    {
            //        Server = "localhost", Port = 1883, TlsOptions = tlsOptions
            //    }
            //};

            //_options = new ManagedMqttClientOptions
            //{
            //    ClientOptions = clientOptions
            //};

            _optionsS = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(
                    new MqttClientOptionsBuilder()
                        .WithClientId("Elsa_Sender")
                        .WithTcpServer("localhost", 1883)
                        .WithCredentials("elsa_user", "password")
                        .Build())
                .Build();

            _optionsR = new ManagedMqttClientOptionsBuilder()
                .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
                .WithClientOptions(
                    new MqttClientOptionsBuilder()
                        //.WithClientId("Elsa_" + Guid.NewGuid().ToString())
                        .WithClientId("Elsa_Receiver")
                        .WithTcpServer("localhost", 1883)
                        .WithCredentials("elsa_user", "password")
                        .Build())
                .Build();
        }


        public async Task<IManagedMqttClientWrapper> GetSenderAsync(string topic, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_senders.TryGetValue(topic, out var messageSender))
                    return messageSender;

                //await EnsureQueueExistsAsync(queueName, cancellationToken);

                var newClient = _factory.CreateManagedMqttClient();

                var newMessageSender = new ManagedMqttClientWrapper(newClient, _optionsS, topic);

                _senders.Add(topic, newMessageSender);
                return newMessageSender;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<IManagedMqttClientWrapper> GetReceiverAsync(string topic, CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_receivers.TryGetValue(topic, out var messageReceiver))
                    return messageReceiver;

                var newClient = _factory.CreateManagedMqttClient();

                var newMessageReceiver = new ManagedMqttClientWrapper(newClient, _optionsR, topic);
                
                _receivers.Add(topic, newMessageReceiver);
                return newMessageReceiver;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task DisposeReceiverAsync(IManagedMqttClientWrapper receiverClient, CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);

            //var key = GetKeyFor(receiverClient);

            try
            {
                _receivers.Remove(receiverClient.Id);
                await receiverClient.StopAsync();
                receiverClient.Dispose();//??
            }
            finally
            {
                _semaphore.Release();
            }
        }

        // private async Task EnsureQueueExistsAsync(string queueName, CancellationToken cancellationToken)
        // {
        //     if (await _managementClient.QueueExistsAsync(queueName, cancellationToken))
        //         return;
        //
        //     await _managementClient.CreateQueueAsync(queueName, cancellationToken);
        // }

        

        // private async Task EnsureTopicExistsAsync(string topicName, CancellationToken cancellationToken)
        // {
        //     if (!await _managementClient.TopicExistsAsync(topicName, cancellationToken))
        //         await _managementClient.CreateTopicAsync(topicName, cancellationToken);
        // }
        //
        // private async Task EnsureTopicAndSubscriptionExistsAsync(string topicName, string subscriptionName, CancellationToken cancellationToken)
        // {
        //     await EnsureTopicExistsAsync(topicName, cancellationToken);
        //
        //     if (!await _managementClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
        //         await _managementClient.CreateSubscriptionAsync(topicName, subscriptionName, cancellationToken);
        // }
        
        //private static string GetKeyFor(IMqttClient receiverClient) => receiverClient.
            // receiverClient switch
            // {
            //     IMessageReceiver messageReceiver => messageReceiver.Path,
            //     ISubscriptionClient subscriptionClient => $"{subscriptionClient.TopicPath}:{subscriptionClient.SubscriptionName}",
            //     _ => throw new ArgumentOutOfRangeException(nameof(receiverClient), receiverClient, null)
            // };
    }
}