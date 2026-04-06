using Serilog;
using Serilog.Configuration;

namespace DistributedStorage.Infrastructure.Logging;

public static class RabbitMqSinkExtensions
{
    public static LoggerConfiguration RabbitMq(
        this LoggerSinkConfiguration sinkConfiguration,
        string hostName = "localhost",
        string exchange = "logs_exchange",
        string userName = "guest",
        string password = "guest")
    {
        var sink = RabbitMqSink.CreateAsync(hostName, exchange, userName, password)
            .GetAwaiter().GetResult();

        return sinkConfiguration.Sink(sink);
    }
}
