using Discord.Commands;
using Discord.WebSocket;

namespace Jinak.CommandHandling;

public class BetterSocketCommandContext : SocketCommandContext
{
    public SocketGuildUser? GuildUser => Message.Author as SocketGuildUser;
    public SocketGuildChannel? GuildChannel => Message.Channel as SocketTextChannel;
    public string Prefix { get; set; }
    private GuildSettings? _guildSettings;

    public GuildSettings GuildSettings
    {
        get
        {
            if (Channel is not SocketGuildChannel) return GuildSettings.DMSettings;
            if (_guildSettings != null)
                return _guildSettings;
            _guildSettings = Mongo.GetOrCreateGuildSettings(Guild.Id);
            return _guildSettings;
        }
    }

    public BetterSocketCommandContext(DiscordSocketClient client, SocketUserMessage msg) : base(client, msg)
    {
    }
}