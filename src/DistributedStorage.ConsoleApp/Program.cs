using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using DistributedStorage.Application.Services;
using DistributedStorage.Application.Strategies;
using DistributedStorage.ConsoleApp.Menus;
using DistributedStorage.Infrastructure.Chunking;
using DistributedStorage.Infrastructure.Hashing;
using DistributedStorage.Infrastructure.Logging;
using DistributedStorage.Infrastructure.Storage;
using DistributedStorage.Persistence.Context;
using DistributedStorage.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "log-.txt");

var rabbitMqSection = configuration.GetSection("RabbitMq");

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .WriteTo.File(
        path: logPath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .WriteTo.RabbitMq(
        hostName: rabbitMqSection["HostName"] ?? "localhost",
        exchange: rabbitMqSection["Exchange"] ?? "logs_exchange",
        userName: rabbitMqSection["UserName"] ?? "guest",
        password: rabbitMqSection["Password"] ?? "guest")
    .CreateLogger();

var services = new ServiceCollection();

// Logging
services.AddLogging(builder =>
{
    builder.ClearProviders();
    builder.AddSerilog(Log.Logger, dispose: false);
});

// Persistence — DbContextFactory ile Singleton servislerle uyumlu
services.AddDbContextFactory<MetadataDbContext>(options =>
{
    options.UseSqlite(
        configuration.GetConnectionString("MetadataDb") ?? "Data Source=metadata.db");
});

// Repositories
services.AddSingleton<IMetadataRepository, MetadataRepository>();

// Infrastructure
services.AddSingleton<IChunker, Chunker>();
services.AddSingleton<IHashService, Sha256HashService>();
services.AddSingleton<IStorageProvider>(sp =>
    new FileSystemStorageProvider(sp.GetRequiredService<ILogger<FileSystemStorageProvider>>()));
services.AddSingleton<IStorageProvider>(sp =>
    new DatabaseStorageProvider(
        sp.GetRequiredService<ILogger<DatabaseStorageProvider>>(),
        configuration.GetConnectionString("ChunkStorageDb") ?? "Data Source=chunkstorage.db"));

// Application
services.AddSingleton<IChunkSizeStrategy, DefaultChunkSizeStrategy>();
services.AddSingleton<IFileUploadService, FileUploadService>();
services.AddSingleton<IFileDownloadService, FileDownloadService>();

var provider = services.BuildServiceProvider();

var uploadService = provider.GetRequiredService<IFileUploadService>();
var downloadService = provider.GetRequiredService<IFileDownloadService>();
var metadataRepo = provider.GetRequiredService<IMetadataRepository>();
var appLogger = provider.GetRequiredService<ILogger<Program>>();

try
{
    Log.Information("{@LogCategory} | Uygulama başlatıldı.", LogCategory.System);

    while (true)
    {
        Console.WriteLine();
        Console.WriteLine("==== Lütfen yapmak istediğiniz işlemi seçiniz ====");
        Console.WriteLine("1) Dosya Yükle");
        Console.WriteLine("2) Dosya İndir");
        Console.WriteLine("0) Çıkış");
        Console.Write("Seçiminiz: ");

        var input = Console.ReadLine();

        switch (input)
        {
            case "1":
                Log.Information("{@LogCategory} | Menü seçimi: Dosya Yükle", LogCategory.System);
                await UploadMenu.DrawAsync(uploadService, appLogger);
                break;

            case "2":
                Log.Information("{@LogCategory} | Menü seçimi: Dosya İndir", LogCategory.System);
                await DownloadMenu.DrawAsync(downloadService, metadataRepo, appLogger);
                break;

            case "0":
                Log.Information("{@LogCategory} | Menü seçimi: Çıkış", LogCategory.System);
                return;

            default:
                Log.Warning("{@LogCategory} | Geçersiz menü seçimi: {Input}", LogCategory.System, input);
                Console.WriteLine("Geçersiz seçim.");
                break;
        }
    }
}
finally
{
    Log.Information("{@LogCategory} | Uygulama kapatılıyor.", LogCategory.System);
    Log.CloseAndFlush();
}
