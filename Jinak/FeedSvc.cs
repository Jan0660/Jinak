using System.Net.WebSockets;
using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Ozse;
using Ozse.Results;
using Websocket.Client;

namespace Jinak;

// todo: namespace/folder for svcs
public static class FeedSvc
{
    public static OzseClient Client { get; set; }

    public static Dictionary<string, Func<ValidateJobResult, Task>> ValidateCallbacks { get; } = new();

    // todo(cleanup)
    public static readonly string[] ValidatableFeeds = { "npm", "pubdev", "github", "twitter", "youtube", "twitch" };

    public class Packet
    {
        public string Type { get; set; }
        public JObject Data { get; set; }
    }

    public class ValidateJobResult
    {
        public bool Valid { get; set; }
        public string? Error { get; set; }
        public Job Job { get; set; }
    }

    public static void Start()
    {
        Client = new(Program.Config.OzseUrl);

        async Task HandleLost()
        {
            Console.Debug("Feed handle lost");
            foreach (var result in await Client.GetResultsAsync())
                await HandleResult(result);
        }

        DumbTimer.Start("FeedHandleLost", 60_000, () => HandleLost());

        var ws = new WebsocketClient(new Uri(Program.Config.OzseUrl.Replace("http", "ws") + "/ws"));
        // ws.OnReadException += exc =>
        // {
        //     Console.Error($"Feed WS Read: {exc}");
        //     Task.Run(async () =>
        //     {
        //         ws.Client.Abort();
        //         Connect();
        //     });
        // };
        ws.MessageReceived += result =>
        {
            if (result.MessageType == WebSocketMessageType.Text)
            {
                // todo(perf): use System.Text.Json
                var packet = JsonConvert.DeserializeObject<Packet>(result.Text,
                    new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() });
                Console.Debug(packet.Type);
                switch (packet.Type)
                {
                    case "new-result":
                    {
                        var r = packet.Data.ToObject<Result>(new JsonSerializer()
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        });
                        Console.Debug(r);
                        HandleResult(r);
                        break;
                    }
                    case "job-validated":
                    {
                        var r = packet.Data.ToObject<ValidateJobResult>(new JsonSerializer()
                        {
                            ContractResolver = new CamelCasePropertyNamesContractResolver()
                        });
                        Console.Debug(r);
                        ValidateCallbacks[r.Job.Id]?.Invoke(r);
                        break;
                    }
                }
            }

            return Task.CompletedTask;
        };

        ws.ReconnectTimeout = TimeSpan.FromSeconds(50);
        ws.ReconnectionHappened += _ =>
        {
            Console.Debug("Reconnected");
            return Task.CompletedTask;
        };
        ws.DisconnectionHappened += _ =>
        {
            Console.Debug("Disconnected");
            return Task.CompletedTask;
        };
        // todo(perf)
        // todo: save ping time
        DumbTimer.Start("FeedWsPing", 30_000, () => ws.Send(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new
        {
            type = "ping",
            data = new
            {
                time = DateTimeOffset.Now.ToUnixTimeMilliseconds()
            }
        })));

        ws.Start();

        // void Connect()
        // {
        //     ws.ConnectAsync().ContinueWith(t =>
        //     {
        //         if (t.Exception is null)
        //             Console.Debug("Feed WS Connected");
        //         else
        //         {
        //             Console.Debug($"Feed WS Connection failed");
        //             Task.Delay(1000).ContinueWith(_ => Connect());
        //         }
        //     });
        // }
        //
        // Connect();
    }

    public static async Task HandleResult(Result result)
    {
        Console.Debug($"feed svc handling: {result.Id}");
        var job = await Client.GetJobAsync(result.JobId);
        var feedSettings = Mongo.FeedCollection.GetOne(f => f.JobId == job.Id);
        if (feedSettings == null)
            return;
        var channel = Program.Discord.GetChannel(feedSettings.ChannelId) as ITextChannel;
        await Client.DeleteResultAsync(result.Id);
        switch (result.JobName)
        {
            case "reddit":
            {
                var data = (RedditResult)result.ParseData()!;
                var embed = new EmbedBuilder()
                {
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = $"New post on r/{data.SubredditName}!"
                    },
                    Url = data.Link,
                    Title = data.Title,
                    ImageUrl = data.ContentUrl,
                    // todo: check if crossposts work without this
                    // post is LinkPost linkPost
                    // ? linkPost.URL.StartsWith("/r/")
                    //     ? null
                    //     : linkPost.URL
                    // : null,
                    Description = data.ContentText,
                    Fields = new List<EmbedFieldBuilder>()
                    {
                        new()
                        {
                            Name = "Author",
                            IsInline = true,
                            Value = data.Author[1..]
                        }
                    },
                    Color = new Color(243, 65, 18)
                };
                if (data.Nsfw | data.Spoiler)
                {
                    if (data.Nsfw && data.Spoiler)
                        embed.AddField("Content Warnings", "NSFW, Spoiler", true);
                    else if (data.Nsfw && !data.Spoiler)
                        embed.AddField("Content Warnings", "NSFW", true);
                    else if (!data.Nsfw && data.Spoiler)
                        embed.AddField("Content Warnings", "Spoiler", true);
                }

                if (data.Nsfw
                    // todo: check if channel is not nsfw
                    //&& subscription.Channel.IsNsfw == false
                   )
                {
                    // post is marked nsfw and channel is not
                    embed.ImageUrl = null;
                    embed.Description =
                        "Post content censored because post is marked NSFW and this channel isn't.";
                }

                if (data.Spoiler)
                {
                    embed.ImageUrl = null;
                    embed.Description = embed.Description != null
                        ? "||" + embed.Description + "||"
                        : "Post marked as spoiler.";
                }

                await channel.SendMessageAsync(embed: embed.Build());
                break;
            }
            case "github":
            {
                var release = (GitHubReleaseResult)result.ParseData()!;
                await channel.SendMessageAsync(embed: new EmbedBuilder
                {
                    Author = new()
                    {
                        Name = $"New GitHub release for {job.Data["owner"]}/{job.Data["repo"]}!"
                    },
                    Title = release.Name,
                    Url = release.HtmlUrl,
                    Description = $@"**Description:**
```md
{(release.Body.Length > 3900 ? release.Body.Remove(3900) + "..." : release.Body)}
```
This release comes with {release.Assets.Length} assets.
**Source code downloads:** [\[Tarball\]]({release.Tarball}) [\[Zipball\]]({release.Zipball})",
                    Footer = new()
                    {
                        Text = "Published at"
                    },
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(release.PublishedAt),
                    ThumbnailUrl = release.AuthorAvatar,
                    Color = new Color(47, 49, 54)
                }.Build());
                break;
            }
            case "pubdev":
            {
                var data = (PubDevResult)result.ParseData()!;
                var release = data.Package;
                await channel.SendMessageAsync(embed: new EmbedBuilder
                {
                    Title = $"New pub.dev release for {release.PubSpec.Name}!",
                    Url = $"https://pub.dev/packages/{release.PubSpec.Name}",
                    Description = $@"`{data.LastVersion}` **=>** `{release.PubSpec.Version}`",
                    Color = new Color(41, 182, 246),
                    Footer = new()
                    {
                        Text = "Published at"
                    },
                    // todo: no idea if this is utc or not
                    Timestamp = release.Published,
                }.Build());
                break;
            }
            case "npm":
            {
                var data = (NpmResult)result.ParseData()!;
                var (previous, latest) = (data.Previous, data.Package);
                var sizeDifference = (latest.Dist.UnpackedSize) - (previous.Dist.UnpackedSize);
                await channel.SendMessageAsync(embed: new EmbedBuilder
                {
                    Title = $"<:npm:791418879724224532> New Npm package release for {latest.Name}!",
                    Description = @$"**Version:** {previous.Version} **=>** {latest.Version}
**File count:** {previous.Dist.FileCount} **=>** {latest.Dist.FileCount}
**Size:** {previous.Dist.UnpackedSize / 1000d}KB **=>** {latest.Dist.UnpackedSize / 1000d}KB **|** **Diff:** {(sizeDifference > 0 ? "+" : "")}{sizeDifference / 1000d}KB
[\[Tarball Download\]]({latest.Dist.Tarball})",
                    Color = new Color(47, 49, 54),
                    // Footer = new()
                    // {
                    //     Text = "Released at"
                    // },
                    // todo
                    // Timestamp = pkg.Times[currentVersion],
                    Author = new EmbedAuthorBuilder()
                    {
                        Name = "By " + latest.Author.Name
                    },
                    Url = $"https://www.npmjs.com/package/{latest.Name}"
                }.Build());
                break;
            }
            case "twitter":
            {
                var data = (TwitterResult)result.ParseData()!;
                // todo: something for quote/retweets?
                await channel.SendMessageAsync(data.Tweet.PermanentUrl);
                break;
            }
            case "youtube":
            {
                var data = (YouTubeResult)result.ParseData()!;
                await channel.SendMessageAsync($"https://youtu.be/{data.Id}", allowedMentions: new());
                break;
            }
            case "twitch":
            {
                var data = (TwitchResult)result.ParseData()!;
                if (data.Type != "goOnline")
                    break;
                await channel.SendMessageAsync($"{data.Item.UserName} is live! https://twitch.tv/{data.Item.UserLogin}",
                    allowedMentions: new());
                break;
            }
            default:
            {
                await channel.SendMessageAsync($"{result.Id}: {result.Data["link"]}");
                break;
            }
        }

        Console.Debug($"feed svc handled: {result.Id}");
    }
}