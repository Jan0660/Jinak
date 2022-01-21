using Discord;
using Color = System.Drawing.Color;

namespace Jinak;

public static class EventHandlers
{
    public static Task Log(LogMessage arg)
    {
        Console.Log(Program.DiscordMessageType, arg.Message, new DiscordLogInfoContext() { Message = arg });
        return Task.CompletedTask;
    }

    public static Task Ready()
    {
        Console.Info("Ready!");
        return Task.CompletedTask;
    }
}

public class DiscordLogInfo : ILogInfo
{
    public string GetValue(LogInfoContext context)
    {
        if (context is DiscordLogInfoContext c)
        {
            Style.Color = c.Message.Severity switch
            {
                LogSeverity.Critical => Color.DarkRed,
                LogSeverity.Error => Color.Red,
                LogSeverity.Warning => Color.Yellow,
                LogSeverity.Info => Color.Cyan,
                LogSeverity.Verbose => Color.White,
                LogSeverity.Debug => Color.White,
                _ => Color.White
            };
            return c.Message.Severity.ToString().ToUpper() ?? "sus";
        }
        else
            System.Console.Write("H");

        return "what";
    }

    public ConsoleStyleOption Style { get; set; } = new();
}

class DiscordLogInfoContext : LogInfoContext
{
    public LogMessage Message { get; init; }
}