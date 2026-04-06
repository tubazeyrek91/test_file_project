using DistributedStorage.LogConsumer.Services;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var rabbitSection = configuration.GetSection("RabbitMq");
var mongoSection = configuration.GetSection("MongoDb");
var consumerSection = configuration.GetSection("Consumer");

var intervalSeconds = int.Parse(consumerSection["IntervalSeconds"] ?? "60");
var batchSize = int.Parse(consumerSection["BatchSize"] ?? "500");

Console.WriteLine("=== Log Consumer başlatılıyor ===");
Console.WriteLine($"Aralık: {intervalSeconds} saniye | Batch: {batchSize}");

await using var consumer = await RabbitMqConsumerService.CreateAsync(
    hostName: rabbitSection["HostName"] ?? "localhost",
    exchange: rabbitSection["Exchange"] ?? "logs_exchange",
    queuePrefix: rabbitSection["QueuePrefix"] ?? "log_queue_",
    userName: rabbitSection["UserName"] ?? "guest",
    password: rabbitSection["Password"] ?? "guest");

var mongoWriter = new MongoDbWriterService(
    connectionString: mongoSection["ConnectionString"] ?? "mongodb://localhost:27017",
    databaseName: mongoSection["DatabaseName"] ?? "distributed_storage_logs",
    collectionPrefix: mongoSection["CollectionPrefix"] ?? "logs_");

Console.WriteLine("RabbitMQ ve MongoDB bağlantıları hazır.");
Console.WriteLine("Çıkmak için Ctrl+C basın.\n");

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    Console.WriteLine("\nKapatılıyor...");
};

while (!cts.Token.IsCancellationRequested)
{
    try
    {
        var totalProcessed = 0;
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var category in consumer.GetCategories())
        {
            var messages = await consumer.ConsumeAsync(category, batchSize);

            if (messages.Count > 0)
            {
                var written = await mongoWriter.WriteAsync(category, messages);
                totalProcessed += written;
                Console.WriteLine($"[{timestamp}] {category}: {written} log MongoDB'ye yazıldı.");
            }

            foreach (var msg in messages)
                msg.Dispose();
        }

        if (totalProcessed == 0)
        {
            Console.WriteLine($"[{timestamp}] Kuyrukta mesaj yok.");
        }
        else
        {
            Console.WriteLine($"[{timestamp}] Toplam: {totalProcessed} log işlendi.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[HATA] {ex.Message}");
    }

    try
    {
        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), cts.Token);
    }
    catch (OperationCanceledException)
    {
        break;
    }
}

Console.WriteLine("Log Consumer kapatıldı.");
