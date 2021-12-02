using System.Text;
using System.Threading.Tasks;
using Elsa.Activities.Mqtt.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using MQTTnet;
using MQTTnet.Client.Disconnecting;

namespace Elsa.Activities.Mqtt
{
    [Trigger(
        Category = "MQTT",
        DisplayName = "Send MQTT Message",
        Description = "Sends MQTT message matching with topic",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    //tODO: base class?
    public class SendMqttMessage : Activity
    {
        private readonly IMessageSenderClientFactory _messageSenderClientFactory;
        
        public SendMqttMessage(IMessageSenderClientFactory messageSenderClientFactory)
        {
            _messageSenderClientFactory = messageSenderClientFactory;
        }
        
        [ActivityInput(
            Hint = "Topic",
            Order = 1,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Topic { get; set; } = default!;

        [ActivityInput(
            Hint = "Message body",
            Order = 2,
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.Json })]
        public string Message { get; set; } = default!;

        [ActivityOutput(Hint = "Received message")]
        public object? Output { get; set; }
        
        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        { 
            var client = await _messageSenderClientFactory.GetSenderAsync(Topic);

            await client.StartAsync();

            //client.SubscribeAsync(Topic);
            while (!client.Client.IsConnected)
            {

            }

            await client.PublishAsync(Topic, Message); // Since 3.0.5 with CancellationToken
            
            await client.StopAsync();

            //client.ConnectAsync()
            // var message = (ApplicationMessage)context.Input;
            //
            // Output = messageBody;
            //
            // context.LogOutputProperty(this, nameof(Output), Output);

            return Done();
        }
    }
}