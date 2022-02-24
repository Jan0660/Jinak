using System.Diagnostics;
using System.Linq.Expressions;
using Discord.Commands;
using Jinak.CommandHandling.Attributes;
using MongoDB.Driver;

namespace Jinak;

public static class ExtensionMethods
{
    public static bool IsHidden(this CommandInfo command)
        => command.Attributes.OfType<HiddenAttribute>().Any();

    public static bool IsHidden(this ModuleInfo module)
        => module.Attributes.OfType<HiddenAttribute>().Any();

    public static T? GetAttribute<T>(this CommandInfo command) where T : Attribute
        => command.Attributes.OfType<T>().FirstOrDefault();

    public static T? GetAttribute<T>(this ModuleInfo module) where T : Attribute
        => module.Attributes.OfType<T>().FirstOrDefault();

    public static readonly FindOptions _getOneOptions = new()
    {
        BatchSize = 1
    };

    public static T? GetOne<T>(this IMongoCollection<T> collection, Expression<Func<T, bool>> filter) where T : class
        => collection.Find(filter, _getOneOptions).FirstOrDefault();

    public static Task PerfLog(this Task task, string name)
    {
#if DEBUG
        var sw = Stopwatch.StartNew();
        task.ContinueWith(t =>
        {
            sw.Stop();
            Console.Debug($"{name} - {sw.ElapsedMilliseconds}ms");
        });
#endif
        return task;
    }

    public static Task<T> PerfLog<T>(this Task<T> task, string name)
    {
#if DEBUG
        var sw = Stopwatch.StartNew();
        task.ContinueWith(t =>
        {
            sw.Stop();
            Console.Debug($"{name} - {sw.ElapsedMilliseconds}ms");
        });
#endif
        return task;
    }

    public static Discord.Color? ToDiscordColor(this System.Drawing.Color? color)
        => color.HasValue ? new(color.Value.R, color.Value.G, color.Value.B) : null;
}