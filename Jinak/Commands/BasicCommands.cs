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
        var messagePing = stopwatch.ElapsedMilliseconds;
        var mongoPing = await Mongo.Ping();
        stopwatch.Restart();
        await FeedSvc.Client.Http.GetAsync("/");
        var ozsePing = stopwatch.ElapsedMilliseconds;
        await msg.ModifyAsync(x => (x.Content, x.Embed) = (null, new EmbedBuilder()
        {
            Title = "Pong!",
            Fields = new()
            {
                new()
                {
                    IsInline = true,
                    Name = "Message",
                    Value = $"{messagePing}ms"
                },
                new()
                {
                    IsInline = true,
                    Name = "WebSocket",
                    Value = $"{Context.Client.Latency}ms"
                },
                new()
                {
                    IsInline = true,
                    Name = "MongoDB",
                    Value = $"{mongoPing}ms"
                },
                new()
                {
                    IsInline = true,
                    Name = "Ozse",
                    Value = $"{ozsePing}ms"
                },
            }
        }.Build()));
    }
}