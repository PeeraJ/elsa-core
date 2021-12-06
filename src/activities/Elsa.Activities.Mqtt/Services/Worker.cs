using Elsa.Activities.Mqtt.Bookmarks;
using Elsa.Activities.Mqtt.Options;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using System.Net.Mqtt;

namespace Elsa.Activities.Mqtt.Services
{
    public class Worker
    {
        private readonly Scoped<IWorkflowLaunchpad> _workflowLaunchpad;
        private readonly Func<IMqttClientWrapper, Task> _disposeReceiverAction;
        private IMqttClientWrapper ReceiverClient { get; }
        
        public Worker(
            IMqttClientWrapper receiverClient,
            Scoped<IWorkflowLaunchpad> workflowLaunchpad,
            Func<IMqttClientWrapper, Task> disposeReceiverAction)
        {
            ReceiverClient = receiverClient;
            _workflowLaunchpad = workflowLaunchpad;
            _disposeReceiverAction = disposeReceiverAction;

            ReceiverClient.SetMessageHandler(OnMessageReceived);
        }

        private string ActivityType => nameof(MqttMessageReceived);
        public async ValueTask DisposeAsync() => await _disposeReceiverAction(ReceiverClient);

        private IBookmark CreateBookmark(MqttClientOptions options) => new MessageReceivedBookmark(options.Topic, options.Host, options.Port, options.Username, options.Password); 

        private async Task TriggerWorkflowsAsync(MqttApplicationMessage message, CancellationToken cancellationToken)
        {
            var bookmark = CreateBookmark(ReceiverClient.Options);
            var launchContext = new WorkflowsQuery(ActivityType, bookmark);
            
            await _workflowLaunchpad.UseServiceAsync(service => service.CollectAndDispatchWorkflowsAsync(launchContext, new WorkflowInput(message), cancellationToken));
        }
        
        private async Task OnMessageReceived(MqttApplicationMessage message) => await TriggerWorkflowsAsync(message, CancellationToken.None);
    }
}