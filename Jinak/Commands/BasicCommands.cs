using System.Diagnostics;
using Discord;
using Discord.Commands;
using Jinak.CommandHandling;

namespace Jinak.Commands;

public class BasicCommands : BetterModuleBase
{
    [Command("ping")]
    public async Task Ping()
    {
        var stopwatch = Stopwatch.StartNew();
        var msg = await ReplyAsync("Measuring...");
        stopwatch.Stop();
        var mongoPing = await Mongo.Ping();
        // todo: ping for Ozse
        await msg.ModifyAsync(x => (x.Content, x.Embed) = (null, new EmbedBuilder()
        {
            Title = "Pong!",
            Fields = new()
            {
                new()
                {
                    IsInline = true,
                    Name = "Message",
                    Value = $"{stopwatch.ElapsedMilliseconds}ms"
                },
                new()
                {
                    IsInline = true,
                    Name = "Whatever Discord",
                    Value = $"{Context.Client.Latency}ms"
                },
                new()
                {
                    IsInline = true,
                    Name = "MongoDB",
                    Value = $"{mongoPing}ms"
                }
            }
        }.Build()));
    }
}