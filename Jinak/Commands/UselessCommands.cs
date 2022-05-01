// using System.Drawing;
// using Discord;
// using Discord.Commands;
// using Jinak.CommandHandling;
//
// namespace Jinak.Commands;
//
// public class UselessCommands : BetterModuleBase
// {
//     
//     [Command("ansi")]
//     [Summary("Turn someone into ANSI.")]
//     public async Task AnsiUser(IUser user)
//     {
//         var mem = new MemoryStream();
//         await AnsiUrl(user.GetAvatarUrl(ImageFormat.Png) ?? user.GetDefaultAvatarUrl(), new StreamWriter(mem), 128);
//         mem.Position = 0;
//         await Context.Channel.SendFileAsync(mem, "the.ansi");
//     }
//
//     [Command("ansi")]
//     [Summary("Turn an emote into ANSI.")]
//     public async Task AnsiUser(string emoteStr)
//     {
//         if (Emote.TryParse(emoteStr, out var emote))
//         {
//             var mem = new MemoryStream();
//             await AnsiUrl(emote.Url, new StreamWriter(mem), 128);
//             mem.Position = 0;
//             await Context.Channel.SendFileAsync(mem, "the.ansi");
//         }
//         else
//             await ReplyAsync("Failed to parse emote.");
//     }
//
//     public async Task AnsiUrl(string url, TextWriter writer, int width)
//     {
//         using var image = await SixLabors.ImageSharp.Image.LoadAsync(await new HttpClient().GetStreamAsync(url));
//         int height = (int)((double)image.Height / image.Width * width);
//         image.Mutate(c => c.Resize(new Size(width, height), new NearestNeighborResampler(), true));
//         var memory = new MemoryStream();
//         await image.SaveAsBmpAsync(memory);
//         var bitmap = new Bitmap(memory);
//         var color = System.Drawing.Color.FromArgb(0, 0, 0);
//         writer.Write($"\x1b[38;2;{color.R};{color.G};{color.B}m");
//         for (int j = 1; j < bitmap.Height; j += 2)
//         {
//             for (int i = 0; i < bitmap.Width; i++)
//             {
//                 writer.Write(Ansi.ForegroundColor(Ansi.BackgroundColor("▀", bitmap.GetPixel(i, j)),
//                     bitmap.GetPixel(i, j - 1)));
//             }
//
//             writer.Write('\n');
//         }
//     }
// }

