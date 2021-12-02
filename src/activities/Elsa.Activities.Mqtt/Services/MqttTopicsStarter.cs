using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;

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
            await DisposeExistingWorkersAsync();//tODO: subscription name alternative?
            var topics = (await GetTopicsAsync(cancellationToken).ToListAsync(cancellationToken)).Distinct();

            foreach (var topic in topics) 
                await CreateAndAddWorkerAsync(topic, cancellationToken);
        }

        private async Task CreateAndAddWorkerAsync(string topic, CancellationToken cancellationToken)
        {
            try
            {
                var receiver = await _receiverFactory.GetReceiverAsync(topic, cancellationToken);
                var worker = ActivatorUtilities.CreateInstance<Worker>(_serviceProvider, receiver, topic, (Func<IManagedMqttClientWrapper, Task>) DisposeReceiverAsync);
                _workers.Add(worker);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Failed to create a receiver for topic {Topic}", topic);
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

        private async Task DisposeReceiverAsync(IManagedMqttClientWrapper messageReceiver) => await _receiverFactory.DisposeReceiverAsync(messageReceiver);

        private async IAsyncEnumerable<string> GetTopicsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
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
                    yield return topic!;
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