using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using MQTTnet;

namespace Elsa.Activities.Mqtt
{
    [Trigger(
        Category = "MQTT",
        DisplayName = "MQTT Message Received",
        Description = "Triggers when MQTT message matching specified topic is received",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class MqttMessageReceived : Activity
    {
        [ActivityInput(
            Hint = "Topic",
            Order = 1,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Topic { get; set; } = default!;
        
        [ActivityOutput(Hint = "Received message")]
        public object? Output { get; set; }
        
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternalAsync(context) : Suspend();
        
        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternalAsync(context);
        
        private IActivityExecutionResult ExecuteInternalAsync(ActivityExecutionContext context)
        {
            var message = (MqttApplicationMessage)context.Input;

            Output = System.Text.Encoding.UTF8.GetString(message.Payload);

            context.LogOutputProperty(this, nameof(Output), Output);

            return Done();


        }
    }
}