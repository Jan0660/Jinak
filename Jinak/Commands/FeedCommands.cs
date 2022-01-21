using Discord.Commands;
using Discord.WebSocket;
using Jinak.CommandHandling;
using Ozse;

namespace Jinak.Commands;

[RequireContext(ContextType.Guild)]
[Group("feed")]
public class FeedCommands : BetterModuleBase
{
    [Command("subscribe")]
    [Alias("sub")]
    public async Task Subscribe(string feedName, SocketTextChannel channel, string? args = null)
    {
        // todo(parity): some kind of limit?
        // todo(error-check)(parity): check if can send messages into (channel)
        Job job;
        switch (feedName)
        {
            case "reddit":
            {
                job = new()
                {
                    Name = "reddit",
                    Timer = 10,
                    AllowTaskDuplicates = false,
                    Data = new()
                    {
                        ["url"] = $"https://www.reddit.com/r/{args}/new/.rss?sort=new"
                    }
                };
                break;
            }
            case "github":
            {
                string owner = null;
                string repo = null;

                bool ParseArgs(char ch)
                {
                    if (args is null)
                        throw new Exception("No arguments provided");
                    var split = args.Split(ch);
                    if (split.Length != 2)
                        return false;
                    owner = split[0];
                    repo = split[1];
                    return true;
                }

                // todo: holy shit this is terrible
                if (!ParseArgs('/'))
                    if (!ParseArgs('\\'))
                        if (!ParseArgs(' '))
                            throw new Exception("Invalid arguments");

                job = new()
                {
                    Name = "github",
                    Timer = 10 * 60,
                    AllowTaskDuplicates = false,
                    Data = new()
                    {
                        ["owner"] = owner,
                        ["repo"] = repo,
                    }
                };
                break;
            }
            case "pub.dev":
            case "pubdev":
            {
                // todo: check if args is a valid pub.dev package name
                job = new()
                {
                    Name = "pubdev",
                    Timer = 10 * 60,
                    AllowTaskDuplicates = false,
                    Data = new()
                    {
                        ["name"] = args,
                        ["lastVersion"] = "h",
                    }
                };
                break;
            }
            case "npm":
            {
                // todo: check if args is a valid npm package name
                job = new()
                {
                    Name = "npm",
                    Timer = 10 * 60,
                    AllowTaskDuplicates = false,
                    Data = new()
                    {
                        ["name"] = args,
                        ["lastVersion"] = "h",
                    }
                };
                break;
            }
            default:
            {
                await ReplyAsync("what");
                return;
            }
        }

        job = await FeedSvc.Client.CreateJobAsync(job).PerfLog("create job");
        await Mongo.FeedCollection.InsertOneAsync(new FeedSettings()
        {
            ChannelId = channel.Id,
            GuildId = Context.Guild.Id,
            JobId = job.Id
        }).PerfLog("feed sub insert");
        Console.Log(job);
        await ReplyAsync("h");
    }
}