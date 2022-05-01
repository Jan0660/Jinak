using Discord;
using Color = System.Drawing.Color;

namespace Jinak;

public static class EventHandlers
{
    public static Task Log(LogMessage arg)
    {
        Console.Logger.PreWrite(arg.Message, Program.DiscordLogType, true,
            new DiscordLogPreContext { Message = arg });
        return Task.CompletedTask;
    }

    public static Task Ready()
    {
        Console.Info("Ready!");
        Program.LogChannel = Program.Discord.GetChannel(Program.Config.LogChannel) as ITextChannel;
        return Task.CompletedTask;
    }
}

public class DiscordLogPre : LogPre
{
    public override Style? Style { get; set; } = new();

    public override bool Write(in LogContext context, ref SpanStringBuilder builder)
    {
        if (context.ExtraContext is DiscordLogPreContext c)
            builder.Append(c.Message.Severity.ToString() ?? "sus");
        else
            System.Console.Write("H");
        return true;
    }

    public override void SetStyle(in LogContext context)
    {
        Style ??= new();
        if (context.ExtraContext is DiscordLogPreContext c)
        {
            if (c.Message.Exception != null)
                Console.Error(c.Message.Exception.ToString());
            Style.ForegroundColor = c.Message.Severity switch
            {
                LogSeverity.Critical => Color.DarkRed,
                LogSeverity.Error => Color.Red,
                LogSeverity.Warning => Color.Yellow,
                LogSeverity.Info => Color.Cyan,
                LogSeverity.Verbose => Color.White,
                LogSeverity.Debug => Color.White,
                _ => Color.White
            };
        }
    }
}

class DiscordLogPreContext
{
    public LogMessage Message { get; init; }
}