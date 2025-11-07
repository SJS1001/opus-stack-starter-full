namespace Opus.Messaging;
public interface IMessageBus
{
    Task PublishAsync<T>(string topic, T message, CancellationToken ct = default);
    Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default);
}
