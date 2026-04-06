using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;

namespace DistributedStorage.LogConsumer.Services;

public class MongoDbWriterService
{
    private readonly IMongoDatabase _database;
    private readonly string _collectionPrefix;

    public MongoDbWriterService(string connectionString, string databaseName, string collectionPrefix)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
        _collectionPrefix = collectionPrefix;
    }

    public async Task<int> WriteAsync(string category, List<JsonDocument> messages)
    {
        if (messages.Count == 0)
            return 0;

        var collectionName = $"{_collectionPrefix}{category}";
        var collection = _database.GetCollection<BsonDocument>(collectionName);

        var documents = new List<BsonDocument>();

        foreach (var msg in messages)
        {
            var bson = BsonDocument.Parse(msg.RootElement.GetRawText());
            documents.Add(bson);
        }

        await collection.InsertManyAsync(documents);
        return documents.Count;
    }
}
