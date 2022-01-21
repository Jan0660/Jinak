using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Jinak;

public class GuildSettings
{
    [BsonId]
    [BsonElement("_id")]
    public ulong Id { get; set; }

    public static readonly GuildSettings DMSettings = new() { };

    public async Task<List<FeedSettings>> GetFeedsAsync()
        => (await Mongo.FeedCollection.Find(f => f.GuildId == Id).ToListAsync());
}

[BsonIgnoreExtraElements]
public class FeedSettings
{
    public string JobId { get; set; }
    public ulong ChannelId { get; set; }
    public ulong GuildId { get; set; }
}