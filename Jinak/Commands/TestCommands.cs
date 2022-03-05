using Discord.Commands;
using Jinak.CommandHandling;

namespace Jinak.Commands;

public class TestCommands : BetterModuleBase
{
    [Command("test")]
    public Task Test()
        => ReplyAsync("sus");

    [Command("testgs")]
    public Task TestGuildSettings()
        => ReplyAsync(Context.GuildSettings.Id.ToString());
}