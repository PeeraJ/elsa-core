namespace Elsa.Activities.Mqtt.Services
{
    public interface IMqttTopicsStarter
    {
        Task CreateWorkersAsync(CancellationToken cancellationToken = default);
    }
}