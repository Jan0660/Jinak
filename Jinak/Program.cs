global using Console = Log73.Console;
global using Log73;
using System.Buffers;
// ReSharper disable once RedundantUsingDirective.Global
using System.Text.Json;
using Discord.WebSocket;
using Discord;
using Discord.Commands;
using Discord.Webhook;
using Jinak.CommandHandling;
using Log73.LogPres;
using Log73.Serialization.NewtonsoftJson;

namespace Jinak;

public static class Program
{
    public static LogType DiscordLogType = new()
    {
        Name = "Discord",
        LogLevel = LogLevel.Fatal,
        LogPreStyle = new()
        {
            ForegroundColor = System.Drawing.Color.FromArgb(114, 137, 218)
        },
        LogPres = new()
        {
            new LogTypeLogPre(),
            new TimeLogPre { Format = "HH:mm:ss:fff" },
        }
    };

    public static Config Config;
    public static DiscordSocketClient Discord;
    public static CommandHandler CommandHandler { get; private set; }
    public static ITextChannel? LogChannel;

    public static async Task Main()
    {
        Config = JsonSerializer.Deserialize<Config>(await File.ReadAllTextAsync("./config.json"))!;

        #region configure log73

        // todo(cleanup): separate static Logging class for configure and DiscordMessageType and it's use?

#if DEBUG
        Console.Options.LogLevel = LogLevel.Debug;
#endif
        Console.Configure.EnableVirtualTerminalProcessing();
        Console.LogTypes.Error.LogPreStyle.AnsiStyle |= AnsiStyle.Invert;
        Console.LogTypes.TraditionalsAddPre(new TimeLogPre() { Format = "HH:mm:ss:fff" });
        Console.Logger.LogFunction = (in LogContext context) =>
        {
            if ((context.LogType?.LogLevel > LogLevel.Debug || context.ExtraContext is DiscordLogPreContext
                {
                    Message.Severity: < LogSeverity.Verbose
                }) && LogChannel is not null)
                LogChannel.SendMessageAsync(embed:
                    new EmbedBuilder
                    {
                        Title = context.LogType?.Name,
                        Description = context.Message.ToString(),
                        Color = context.LogType?.LogPreStyle?.ForegroundColor.ToDiscordColor()
                    }.Build()
                );
            Console.Logger.AnsiConsoleLogFunction(context);
        };
        DiscordLogType.LogPres!.Add(new DiscordLogPre());
        Console.Configure.UseNewtonsoftJson();

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
        Console.Info("Installed commands and connected to MongoDB.");
        FeedSvc.Start();

        await Task.Delay(-1);
    }
}