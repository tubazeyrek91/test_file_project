using DistributedStorage.Application.Interfaces;
using DistributedStorage.Application.Logging;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace DistributedStorage.Infrastructure.Storage;

public class DatabaseStorageProvider : IStorageProvider
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseStorageProvider> _logger;

    public DatabaseStorageProvider(ILogger<DatabaseStorageProvider> logger, string connectionString = "Data Source=chunkstorage.db")
    {
        _connectionString = connectionString;
        _logger = logger;
        EnsureTableCreated();
    }

    public string Name => "Database";

    private void EnsureTableCreated()
    {
        using var connection = new SqliteConnection(_connectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS ChunkData (
                Key TEXT PRIMARY KEY,
                Data BLOB NOT NULL
            )";
        command.ExecuteNonQuery();

        _logger.LogInformation("{@LogCategory} | Database storage tablosu hazır.", LogCategory.Storage);
    }

    public async Task SaveAsync(string key, byte[] data)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "INSERT OR REPLACE INTO ChunkData (Key, Data) VALUES ($key, $data)";
        command.Parameters.AddWithValue("$key", key);
        command.Parameters.AddWithValue("$data", data);
        await command.ExecuteNonQueryAsync();

        _logger.LogInformation("{@LogCategory} | Chunk veritabanına yazıldı. Key: {Key}, Boyut: {Size} byte",
            LogCategory.Storage, key, data.Length);
    }

    public async Task<byte[]> ReadAsync(string key)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Data FROM ChunkData WHERE Key = $key";
        command.Parameters.AddWithValue("$key", key);

        var result = await command.ExecuteScalarAsync();
        var data = (byte[])result!;

        _logger.LogInformation("{@LogCategory} | Chunk veritabanından okundu. Key: {Key}, Boyut: {Size} byte",
            LogCategory.Storage, key, data.Length);
        return data;
    }
}
