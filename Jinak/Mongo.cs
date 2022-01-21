using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Jinak;

public static class Mongo
{
    public static MongoClient Client { get; private set; }
    public static IMongoDatabase Database { get; private set; }
    public static IMongoCollection<GuildSettings> GuildCollection { get; private set; }
    public static IMongoCollection<FeedSettings> FeedCollection { get; private set; }

    public static async Task ConnectAsync()
    {
        Client = new MongoClient(Program.Config.MongoUrl);
        Database = Client.GetDatabase(Program.Config.MongoDatabase);
        GuildCollection = Database.GetCollection<GuildSettings>("guilds");
        FeedCollection = Database.GetCollection<FeedSettings>("feeds");
    }

    public static async Task<long> Ping()
    {
        var stopwatch = Stopwatch.StartNew();
        await Database.RunCommandAsync((Command<BsonDocument>)"{ping:1}");
        return stopwatch.ElapsedMilliseconds;
    }

    public static GuildSettings GetOrCreateGuildSettings(ulong id)
    {
        var result = GuildCollection.GetOne(g => g.Id == id);
        if (result != null)
            return result;
        result = new()
        {
            Id = id
        };
        GuildCollection.InsertOne(result);
        return result;
    }
}