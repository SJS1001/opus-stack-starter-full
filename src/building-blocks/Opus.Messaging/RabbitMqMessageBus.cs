using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Opus.Messaging;

public sealed class RabbitMqMessageBus : IMessageBus, IDisposable
{
    private readonly IConnection _conn;
    private readonly IModel _ch;

    public RabbitMqMessageBus(string uri)
    {
        var factory = new ConnectionFactory { Uri = new Uri(uri) };
        _conn = factory.CreateConnection();
        _ch = _conn.CreateModel();
    }

    public Task PublishAsync<T>(string topic, T message, CancellationToken ct = default)
    {
        _ch.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);
        var payload = JsonSerializer.SerializeToUtf8Bytes(message);
        _ch.BasicPublish(topic, routingKey: string.Empty, body: payload);
        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(string topic, Func<T, Task> handler, CancellationToken ct = default)
    {
        _ch.ExchangeDeclare(topic, ExchangeType.Fanout, durable: true);
        var q = _ch.QueueDeclare().QueueName;
        _ch.QueueBind(q, topic, routingKey: string.Empty);
        var consumer = new AsyncEventingBasicConsumer(_ch);
        consumer.Received += async (_, ea) =>
        {
            var msg = JsonSerializer.Deserialize<T>(ea.Body.Span)!;
            await handler(msg);
        };
        _ch.BasicConsume(q, autoAck: true, consumer);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _ch.Dispose();
        _conn.Dispose();
    }
}
