using Elsa.Activities.Mqtt.Options;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Elsa.Activities.Mqtt.Services
{
    public class MqttTopicsStarter : IMqttTopicsStarter
    {
        private readonly IMessageReceiverClientFactory _receiverFactory;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MqttTopicsStarter> _logger;
        private readonly ICollection<Worker> _workers;

        public MqttTopicsStarter(
            IMessageReceiverClientFactory receiverFactory,
            IServiceScopeFactory scopeFactory,
            IServiceProvider serviceProvider,
            ILogger<MqttTopicsStarter> logger)
        {
            _receiverFactory = receiverFactory;
            _scopeFactory = scopeFactory;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _workers = new List<Worker>();
        }

        public async Task CreateWorkersAsync(CancellationToken cancellationToken)
        {
            await DisposeExistingWorkersAsync();
            var configs = (await GetConfigurationsAsync(cancellationToken).ToListAsync(cancellationToken)).Distinct();

            foreach (var config in configs) 
                await CreateAndAddWorkerAsync(config, cancellationToken);
        }

        private async Task CreateAndAddWorkerAsync(MqttClientOptions config, CancellationToken cancellationToken)
        {
            try
            {
                var receiver = await _receiverFactory.GetReceiverAsync(config, cancellationToken);
                var worker = ActivatorUtilities.CreateInstance<Worker>(_serviceProvider, receiver, (Func<IMqttClientWrapper, Task>) DisposeReceiverAsync);
                _workers.Add(worker);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to create a receiver for topic {Topic}", config.Topic);
            }
        }

        private async Task DisposeExistingWorkersAsync()
        {
            foreach (var worker in _workers.ToList())
            {
                await worker.DisposeAsync();
                _workers.Remove(worker);
            }
        }

        private async Task DisposeReceiverAsync(IMqttClientWrapper messageReceiver) => await _receiverFactory.DisposeReceiverAsync(messageReceiver);

        private async IAsyncEnumerable<MqttClientOptions> GetConfigurationsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var workflowRegistry = scope.ServiceProvider.GetRequiredService<IWorkflowRegistry>();
            var workflowBlueprintReflector = scope.ServiceProvider.GetRequiredService<IWorkflowBlueprintReflector>();
            var workflowInstanceStore = scope.ServiceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflows = await workflowRegistry.ListActiveAsync(cancellationToken);

            var query =
                from workflow in workflows
                from activity in workflow.Activities
                where activity.Type == nameof(MqttMessageReceived)
                select workflow;

            foreach (var workflow in query)
            {
                // If a workflow is not published, only consider it for processing if it has at least one non-ended workflow instance.
                if (!workflow.IsPublished && !await WorkflowHasNonFinishedWorkflowsAsync(workflow, workflowInstanceStore, cancellationToken))
                    continue;
                
                var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(scope.ServiceProvider, workflow, cancellationToken);

                foreach (var activity in workflowBlueprintWrapper.Filter<MqttMessageReceived>())
                {
                    var topic = await activity.EvaluatePropertyValueAsync(x => x.Topic, cancellationToken);
                    var host = await activity.EvaluatePropertyValueAsync(x => x.Host, cancellationToken);
                    var port = await activity.EvaluatePropertyValueAsync(x => x.Port, cancellationToken);
                    var username = await activity.EvaluatePropertyValueAsync(x => x.Username, cancellationToken);
                    var password = await activity.EvaluatePropertyValueAsync(x => x.Password, cancellationToken);
                    var qos = await activity.EvaluatePropertyValueAsync(x => x.QualityOfService, cancellationToken);

                    yield return new MqttClientOptions(topic!, host!, port!, username!, password!, qos);
                }
            }
        }
        
        private static async Task<bool> WorkflowHasNonFinishedWorkflowsAsync(IWorkflowBlueprint workflowBlueprint, IWorkflowInstanceStore workflowInstanceStore, CancellationToken cancellationToken)
        {
            var count = await workflowInstanceStore.CountAsync(new UnfinishedWorkflowSpecification().WithWorkflowDefinition(workflowBlueprint.Id), cancellationToken);
            return count > 0;
        }
    }
}