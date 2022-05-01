using System.Diagnostics;
using System.Net;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Jinak.CommandHandling;
using SkiaSharp;

namespace Jinak.Commands;

public class UtilityCommands : BetterModuleBase
{
    public async Task<(bool isAnime, decimal certainty, long EvaluationTime)> IsAnimeRequest(string url)
    {
        // download the file
        var webClient = new WebClient();
        var bytes = await webClient.DownloadDataTaskAsync(url);
        // todo: use a Windows temp path or Linux /tmp
        if (!Directory.Exists("./temp"))
            Directory.CreateDirectory("./temp");
        // save it as png
        var fileName = $"./temp/{new Random().Next()}.png";
        var stream = new FileStream(fileName, FileMode.Create);
        SKBitmap.Decode(bytes).Encode(stream, SKEncodedImageFormat.Png, 90);
        await stream.FlushAsync();
        var fullPath = Path.GetFullPath(fileName);
        // send the request to the AnimeAI/aaa.py webserver running
        var http = new HttpClient();
        var req = new HttpRequestMessage(HttpMethod.Get, Program.Config.AnimeAiUrl);
        // header "Fuck" specifies file path to take file from
        req.Headers.Add("Fuck", fullPath);
        var stopwatch = Stopwatch.StartNew();
        var res = await http.SendAsync(req);
        var content = await res.Content.ReadAsStringAsync();
        // give me an excuse to say ai good
        var certainty = Decimal.Parse(content.Split(';')[1]);
        if (content.StartsWith("NotAnime") && certainty == 100.0M)
            certainty = 99.99M;
        return (!content.StartsWith("NotAnime"), certainty, stopwatch.ElapsedMilliseconds);
    }

    [Command("isAnime")]
    public async Task IsAnime(SocketUser user)
    {
        var url = user.GetAvatarUrl(ImageFormat.WebP, 128) ?? user.GetDefaultAvatarUrl();
        var (isAnime, certainty, evaluationTime) =
            await IsAnimeRequest(url);
        EmbedBuilder embed;
        if (isAnime)
            embed = new()
            {
                Title = "WEEB DETECTED",
                Description = $"{user} is a **WEEB**!",
                Color = Color.Green,
            };
        else
            embed = new()
            {
                Title = "BAD",
                Description =
                    $"{user} is **NOT** a **WEEB**!",
                Color = Color.Red,
            };
        embed.WithFooter($"Certainty: {certainty}% Evaluated in {evaluationTime}ms");
        embed.WithThumbnailUrl(url);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("isAnime")]
    public async Task IsAnime()
    {
        if (!Context.Message.Attachments.Any())
        {
            await ReplyAsync("No attachment given.");
            return;
        }

        var url = Context.Message.Attachments.First().ProxyUrl;
        var (isAnime, certainty, evaluationTime) =
            await IsAnimeRequest(url);
        EmbedBuilder embed;
        if (isAnime)
            embed = new()
            {
                Title = "WEEB",
                Description = $"{Context.User.Mention} posted **WEEB SHIT**!",
                Color = Color.Green,
            };
        else
            embed = new()
            {
                Title = "BAD",
                Description = $"{Context.User.Mention} hasn't posted **WEEB SHIT**!",
                Color = Color.Red,
            };
        embed.WithFooter($"Certainty: {certainty}% Evaluated in {evaluationTime}ms");
        embed.WithThumbnailUrl(url);

        await ReplyAsync(embed: embed.Build());
    }
}