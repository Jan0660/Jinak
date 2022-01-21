global using Console = Log73.Console;
global using Log73;
global using Log73.Extensions;
// ReSharper disable once RedundantUsingDirective.Global
global using Log73.ExtensionMethod;
using System.Text.Json;
using Discord.WebSocket;
using Log73.ColorSchemes;
using MessageType = Log73.MessageType;
using Discord;
using Discord.Commands;
using Jinak.CommandHandling;
using Ozse;

namespace Jinak;

public static class Program
{
    public static MessageType DiscordMessageType = new()
    {
        Name = "Discord",
        LogType = LogType.Error,
        Style = new()
        {
            Color = System.Drawing.Color.FromArgb(114, 137, 218)
        }
    };

    public static Config Config;
    public static DiscordSocketClient Discord;
    public static CommandHandler CommandHandler { get; private set; }

    public static async Task Main()
    {
        Config = JsonSerializer.Deserialize<Config>(await File.ReadAllTextAsync("./config.json"))!;

        #region configure log73

        // todo(cleanup): separate static Logging class for configure and DiscordMessageType and it's use?

#if DEBUG
        Console.Options.LogLevel = LogLevel.Debug;
        Console.Options.UseAnsi = false;
        Console.Options.ColorScheme = new RiderDarkMelonColorScheme();
#endif
        MessageTypes.Error.Style.Invert = true;
        Console.Configure.UseNewtonsoftJson();
        DiscordMessageType.LogInfos.Add(new DiscordLogInfo());
        foreach (var msgType in MessageTypes.AsArray().Append(DiscordMessageType))
        {
            msgType.LogInfos.Add(new CallingMethodLogInfo(){UnableToGet = null});
            msgType.LogInfos.Add(
                new TimeLogInfo("HH:mm:ss:fff") { Style = new() { Color = System.Drawing.Color.Gold } });
        }

        #endregion

        // initialize discord client
        Discord = new DiscordSocketClient(new DiscordSocketConfig()
        {
            LogLevel = LogSeverity.Info,
            MessageCacheSize = 1000,
            GatewayIntents =
                GatewayIntents.All // ^ (GatewayIntents.GuildInvites | GatewayIntents.GuildScheduledEvents),
        });
        Discord.Log += EventHandlers.Log;
        Discord.Ready += EventHandlers.Ready;

        // login
        await Discord.LoginAsync(TokenType.Bot, Config.Token);
        await Discord.StartAsync();
        // commands
        CommandHandler = new CommandHandler(Discord, new CommandService(new CommandServiceConfig()
        {
            CaseSensitiveCommands = false,
            DefaultRunMode = RunMode.Async,
            LogLevel = LogSeverity.Debug
        }));
        CommandHandler.Service.Log += EventHandlers.Log;
        await Task.WhenAll(CommandHandler.InstallCommandsAsync(), Mongo.ConnectAsync());
        Console.Log("Installed commands and connected to MongoDB.");
        FeedSvc.Start();

        await Task.Delay(-1);
    }
}