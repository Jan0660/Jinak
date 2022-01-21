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
        // var image = SixLabors.ImageSharp.Image.Load(bytes);
        // todo: use a Windows temp path or Linux /tmp
        if (!Directory.Exists("./temp"))
            Directory.CreateDirectory("./temp");
        var fileName = $"./temp/{new Random().Next()}.png";
        // SKImage.FromEncodedData(bytes);
        var stream = new FileStream(fileName, FileMode.Create);
        SKBitmap.Decode(bytes).Encode(stream, SKEncodedImageFormat.Png, 90);
        await stream.FlushAsync();
        // save it as png
        // await image.SaveAsPngAsync(fileName);
        var fullPath = Path.GetFullPath(fileName);
        // send the request to the AnimeAI/aaa.py webserver running
        var http = new HttpClient();
        // var aps = "http://janh:6600/";
        var aps = "http://localhost:6600/";
        var req = new HttpRequestMessage(HttpMethod.Get, aps);
        req.Headers.Add("Fuck", fullPath);
        // var client = new RestClient("http://localhost:6660/");
        // var req = new RestRequest("/");
        // header "Fuck" specifies file path to take file from
        // req.AddHeader("Fuck", fullPath);
        var stopwatch = Stopwatch.StartNew();
        var res = await http.SendAsync(req);
        var content = await res.Content.ReadAsStringAsync();
        // give me an excuse to say ai good
        if (content.StartsWith("NotAnime") && Math.Round(decimal.Parse(content.Split(';')[1]), 2) == 100.0M)
            content = content.Replace(content.Split(';')[1], "99.99");
        return (!content.StartsWith("NotAnime"), Decimal.Parse(content.Split(';')[1]), stopwatch.ElapsedMilliseconds);
    }

    [Command("isanime")]
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
                Description = $"{user.Username} is a **WEEB**!",
                Color = Color.Green,
            };
        else
            embed = new()
            {
                Title = "BAD",
                Description =
                    $"{user.Username} is **NOT** a **WEEB**!",
                Color = Color.Red,
            };
        embed.WithFooter($"Certainty: {certainty}% Evaluated in {evaluationTime}ms");
        embed.WithThumbnailUrl(url);

        await ReplyAsync(embed: embed.Build());
    }

    [Command("isanime")]
    public async Task IsAnime()
    {
        if (!Context.Message.Attachments.Any())
        {
            await ReplyAsync("tfw no attachments");
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