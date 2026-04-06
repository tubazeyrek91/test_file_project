using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Serilog.Core;
using Serilog.Events;

namespace DistributedStorage.Infrastructure.Logging;

public class RabbitMqSink : ILogEventSink, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly string _exchange;
    private bool _disposed;

    private RabbitMqSink(IConnection connection, IChannel channel, string exchange)
    {
        _connection = connection;
        _channel = channel;
        _exchange = exchange;
    }

    public static async Task<RabbitMqSink> CreateAsync(
        string hostName = "localhost",
        string exchange = "logs_exchange",
        string userName = "guest",
        string password = "guest")
    {
        var factory = new ConnectionFactory
        {
            HostName = hostName,
            UserName = userName,
            Password = password
        };

        var connection = await factory.CreateConnectionAsync();
        var channel = await connection.CreateChannelAsync();

        await channel.ExchangeDeclareAsync(
            exchange: exchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false);

        return new RabbitMqSink(connection, channel, exchange);
    }

    public void Emit(LogEvent logEvent)
    {
        if (_disposed) return;

        var category = "System";
        if (logEvent.Properties.TryGetValue("LogCategory", out var categoryValue))
        {
            category = categoryValue.ToString().Trim('"');
        }

        var message = new
        {
            Timestamp = logEvent.Timestamp.UtcDateTime,
            Level = logEvent.Level.ToString(),
            Category = category,
            Message = logEvent.RenderMessage(),
            Exception = logEvent.Exception?.ToString(),
            Properties = logEvent.Properties
                .Where(p => p.Key != "LogCategory")
                .ToDictionary(p => p.Key, p => p.Value.ToString().Trim('"'))
        };

        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var routingKey = $"log.{category.ToLowerInvariant()}";

        _channel.BasicPublishAsync(
            exchange: _exchange,
            routingKey: routingKey,
            body: body).AsTask().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _channel.CloseAsync().GetAwaiter().GetResult();
        _connection.CloseAsync().GetAwaiter().GetResult();
        _channel.Dispose();
        _connection.Dispose();
    }
}
