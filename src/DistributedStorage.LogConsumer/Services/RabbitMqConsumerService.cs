using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace DistributedStorage.LogConsumer.Services;

public class RabbitMqConsumerService : IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchange;
    private readonly string _queuePrefix;
    private readonly string[] _categories;
    private bool _initialized;

    private static readonly string[] AllCategories =
        ["upload", "download", "chunking", "storage", "hashing", "metadata", "system"];

    private RabbitMqConsumerService(IConnection connection, IChannel channel, string exchange, string queuePrefix)
    {
        _connection = connection;
        _channel = channel;
        _exchange = exchange;
        _queuePrefix = queuePrefix;
        _categories = AllCategories;
    }

    public static async Task<RabbitMqConsumerService> CreateAsync(
        string hostName, string exchange, string queuePrefix, string userName, string password)
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        var service = new RabbitMqConsumerService(connection, channel, exchange, queuePrefix);
        await service.InitializeQueuesAsync();
        return service;
    }

    private async Task InitializeQueuesAsync()
    {
        if (_initialized) return;

        await _channel.ExchangeDeclareAsync(
            exchange: _exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        foreach (var category in _categories)
        {
            var queueName = $"{_queuePrefix}{category}";
            var routingKey = $"log.{category}";

            await _channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            await _channel.QueueBindAsync(
                queue: queueName,
                exchange: _exchange,
                routingKey: routingKey);
        }

        _initialized = true;
    }

    public async Task<List<JsonDocument>> ConsumeAsync(string category, int batchSize)
    {
        var queueName = $"{_queuePrefix}{category}";
        var messages = new List<JsonDocument>();

        for (int i = 0; i < batchSize; i++)
        {
            var result = await _channel.BasicGetAsync(queueName, autoAck: false);
            if (result == null)
                break;

            try
            {
                var json = Encoding.UTF8.GetString(result.Body.Span);
                var doc = JsonDocument.Parse(json);
                messages.Add(doc);
                await _channel.BasicAckAsync(result.DeliveryTag, multiple: false);
            }
            catch
            {
                await _channel.BasicNackAsync(result.DeliveryTag, multiple: false, requeue: true);
            }
        }

        return messages;
    }

    public string[] GetCategories() => _categories;

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
        _channel.Dispose();
        _connection.Dispose();
    }
}
