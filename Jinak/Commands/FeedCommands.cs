using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Jinak.CommandHandling;
using MongoDB.Driver;
using Ozse;

namespace Jinak.Commands;

[RequireContext(ContextType.Guild)]
[Group("feed")]
public class FeedCommands : BetterModuleBase
{
    // modified from https://stackoverflow.com/a/65726047/12520276
    public static readonly Regex YouTubeChannelUrlRegex =
        new("^https?://(www\\.)?youtube\\.com/(channel/((UC[\\w-]{21}[AQgw])|([\\w-]+))|(c/|user/)?[\\w-]+)/?$",
            RegexOptions.Compiled);

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
            case "twitter":
            {
                // todo: check valid user
                job = new()
                {
                    Name = "twitter",
                    Timer = 10 * 60,
                    AllowTaskDuplicates = false,
                    Data = new()
                    {
                        ["name"] = args,
                        ["lastId"] = "1",
                    }
                };
                break;
            }
            case "twitch":
            {
                job = new()
                {
                    Name = "twitch",
                    Timer = 60,
                    AllowTaskDuplicates = false,
                    Data = new()
                    {
                        ["users"] = new[] { args },
                        ["onlineIds"] = Array.Empty<string>(),
                    }
                };
                break;
            }
            case "youtube":
            {
                string name;
                if (args.StartsWith("https://"))
                {
                    var match = YouTubeChannelUrlRegex.Match(args);
                    if (!match.Success)
                        throw new Exception("Invalid channel URL");
                    var index = match.Groups[2].Value.IndexOf('/') + 1;
                    if (index == -1)
                        index = 0;
                    name = match.Groups[2].Value[index..];
                }
                else
                    name = args;

                job = new()
                {
                    Name = "youtube",
                    Timer = 10 * 60,
                    AllowTaskDuplicates = false,
                    Data = new()
                    {
                        ["name"] = name,
                        ["lastId"] = "1",
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

        IUserMessage? msg = null;

        async Task callback()
        {
            job = await FeedSvc.Client.CreateJobAsync(job).PerfLog("create job");
            await Mongo.FeedCollection.InsertOneAsync(new FeedSettings()
            {
                ChannelId = channel.Id,
                GuildId = Context.Guild.Id,
                JobId = job.Id,
            }).PerfLog("feed sub insert");
            Console.Debug(job);
            var embed = new EmbedBuilder()
            {
                Title = "Feed Subscribed",
                Description = $"{job.Name} feed subscribed to {channel.Mention}",
                Color = Color.Green,
            }.Build();
            if (msg == null)
                await ReplyAsync(embed: embed);
            else
                await msg.ModifyAsync(m => m.Embed = embed);
        }

        if (FeedSvc.ValidatableFeeds.Contains(job.Name))
        {
            msg = await ReplyAsync(embed: new EmbedBuilder()
            {
                Title = "Validating...",
                Color = Color.Blue,
            }.Build());
            // todo: use something normal
            job.Id = RandomNumberGenerator.GetInt32(int.MinValue, int.MaxValue).ToString();
            FeedSvc.ValidateCallbacks.Add(job.Id, async validateResult =>
            {
                if (validateResult.Valid)
                    await callback();
                else
                    await msg.ModifyAsync(m => m.Embed = new EmbedBuilder()
                    {
                        Title = "Validation Failed",
                        Description = validateResult.Error,
                        Color = Color.Red,
                    }.Build());
            });
            await FeedSvc.Client.Http.PostAsJsonAsync("/jobs/validate", job);
            return;
        }

        await callback();
    }

    [Command("list")]
    [Alias("ls")]
    public async Task List()
    {
        var c = Mongo.FeedCollection.Find(fs => fs.GuildId == Context.Guild.Id);
        var str = new StringBuilder();
        await Mongo.FeedCollection.Find(fs => fs.GuildId == Context.Guild.Id).ForEachAsync(fs =>
        {
            str.AppendLine($"• <#{fs.ChannelId}> - `{fs.JobId}`");
        });
        await ReplyAsync(str.ToString());
    }
}