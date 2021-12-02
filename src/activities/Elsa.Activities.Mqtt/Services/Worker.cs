using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using MQTTnet.Extensions.ManagedClient;

namespace Elsa.Activities.Mqtt.Services
{
    public class Worker
    {
        private readonly Scoped<IWorkflowLaunchpad> _workflowLaunchpad;
        private readonly Func<IManagedMqttClientWrapper, Task> _disposeReceiverAction;
        private readonly ILogger _logger;//tODO: log
        
        public Worker(
            IManagedMqttClientWrapper receiverClient,
            string topic,
            Scoped<IWorkflowLaunchpad> workflowLaunchpad,
            Func<IManagedMqttClientWrapper, Task> disposeReceiverAction,
            ILogger<Worker> logger)
        {
            ReceiverClient = receiverClient;
            _workflowLaunchpad = workflowLaunchpad;
            _disposeReceiverAction = disposeReceiverAction;
            _logger = logger;


            //tODO: move

            ReceiverClient.StartAsync();

            ReceiverClient.SubscribeAsync(topic);
            ReceiverClient.AddMessageHandler(OnMessageReceived);
        }

        private IManagedMqttClientWrapper ReceiverClient { get; }
        private string ActivityType => nameof(MqttMessageReceived);
        public async ValueTask DisposeAsync() => await _disposeReceiverAction(ReceiverClient);

        private IBookmark CreateBookmark(string topic) => new MessageReceivedBookmark(topic); 

        private async Task TriggerWorkflowsAsync(MqttApplicationMessage message, CancellationToken cancellationToken)
        {
            var bookmark = CreateBookmark(message.Topic);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark);
            
            await _workflowLaunchpad.UseServiceAsync(service => service.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(message), cancellationToken));
        }
        
        private async Task OnMessageReceived(MqttApplicationMessage message)
        {
            await TriggerWorkflowsAsync(message, CancellationToken.None);

            await ReceiverClient.StopAsync();

            //await ReceiverClient.DisconnectAsync(CancellationToken.None);//tODO? properly dispose??
        }
    }
}